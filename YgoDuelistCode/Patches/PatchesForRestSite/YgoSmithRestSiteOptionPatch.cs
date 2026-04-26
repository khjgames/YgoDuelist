using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRestSite;

[HarmonyPatch(typeof(SmithRestSiteOption), MethodType.Constructor, typeof(Player))]
public static class YgoSmithRestSiteOptionEnabledPatch
{
    [HarmonyPostfix]
    public static void Postfix(Player owner, SmithRestSiteOption __instance)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(owner))
            return;

        CardPile? extra = YgoPlayerRunPiles.RunExtraDeck(owner);
        if (extra == null)
            return;
        if (extra.Cards.Any(c => c is FusionMonsterCard && c.IsUpgradable))
            __instance.IsEnabled = true;
    }
}

[HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.OnSelect))]
public static class YgoSmithRestSiteOptionOnSelectPatch
{
    [HarmonyPrefix]
    public static bool Prefix(SmithRestSiteOption __instance, ref Task<bool> __result)
    {
        Player? owner = Traverse.Create(__instance).Property<Player>("Owner").Value;
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(owner))
            return true;

        __result = RunYgoSmithAsync(__instance, owner);
        return false;
    }

    private static bool ShouldSelectLocalCard(Player player) =>
        LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;

    /// <summary>
    /// Same flow as <see cref="CardSelectCmd.FromDeckForUpgrade"/>, but with a caller-built card list (deck + extra-deck fusions).
    /// Uses <see cref="PlayerChoiceResult.FromIndexes"/> for net sync — <see cref="PlayerChoiceResult.FromMutableDeckCards"/> only supports
    /// cards in <see cref="PileType.Deck"/> (<see cref="NetDeckCard.FromModel"/> rejects extra-deck piles).
    /// </summary>
    private static async Task<IEnumerable<CardModel>> SelectForUpgradeAsync(Player player, CardSelectorPrefs prefs, List<CardModel> list)
    {
        if (list.Count <= prefs.MinSelect && !prefs.RequireManualConfirmation)
            return list;

        using IDisposable expectationScope = GridCombatMpExpectation.Push(new GridCombatMpExpectation.Active
        {
            OwnerNetId = player.NetId,
            MinSelect = prefs.Cancelable ? 0 : prefs.MinSelect,
            MaxSelect = prefs.MaxSelect,
            CandidateRowCount = list.Count,
            AllowIndex = true,
            AllowNegativeIndex = prefs.Cancelable
        });
        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        List<CardModel> result;
        if (ShouldSelectLocalCard(player))
        {
            if (CardSelectCmd.Selector != null)
            {
                result = (await CardSelectCmd.Selector.GetSelectedCards(list, prefs.MinSelect, prefs.MaxSelect)).ToList();
            }
            else
            {
                NDeckUpgradeSelectScreen screen = NDeckUpgradeSelectScreen.ShowScreen(list, prefs, player.RunState);
                result = (await screen.CardsSelected()).ToList();
            }

            List<int> indexes = result.Select(c => list.IndexOf(c)).ToList();
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                player,
                choiceId,
                PlayerChoiceResult.FromIndexes(indexes));
        }
        else
        {
            PlayerChoiceResult remoteResult =
                await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId);
            if (remoteResult.ChoiceType == PlayerChoiceType.Index)
            {
                List<int> indexes = remoteResult.AsIndexes().ToList();
                if (indexes.Any(i => i < 0 || i >= list.Count))
                {
                    Godot.GD.PrintErr(
                        $"[YgoDuelist][MP][RestSiteSmith] Remote Index out of range choiceId={choiceId} ownerNet={player.NetId} indexes=[{string.Join(",", indexes)}] candidates={list.Count}");
                    return [];
                }

                result = indexes.Select(i => list[i]).ToList();
            }
            else if (remoteResult.ChoiceType == PlayerChoiceType.DeckCard)
            {
                List<CardModel> deckCards = remoteResult.AsDeckCards().ToList();
                Godot.GD.PrintErr(
                    $"[YgoDuelist][MP][RestSiteSmith] Received DeckCard wire for YGO smith choiceId={choiceId} ownerNet={player.NetId}; accepting deck-card fallback and matching into candidates.");
                result = deckCards
                    .Select(card => list.FirstOrDefault(candidate => ReferenceEquals(candidate, card) || candidate.Id == card.Id))
                    .Where(card => card != null)
                    .Cast<CardModel>()
                    .ToList();
            }
            else
            {
                Godot.GD.PrintErr(
                    $"[YgoDuelist][MP][RestSiteSmith] Unexpected PlayerChoiceType {remoteResult.ChoiceType} choiceId={choiceId} ownerNet={player.NetId}; treating as cancel.");
                result = [];
            }
        }

        return result;
    }

    private static async Task<bool> RunYgoSmithAsync(SmithRestSiteOption option, Player owner)
    {
        int smithCount = Traverse.Create(option).Property<int>(nameof(SmithRestSiteOption.SmithCount)).Value;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, smithCount)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        List<CardModel> candidates = owner.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        CardPile? extra = YgoPlayerRunPiles.RunExtraDeck(owner);
        if (extra == null)
            return false;
        candidates.AddRange(extra.Cards.Where(c => c is FusionMonsterCard && c.IsUpgradable));

        if (candidates.Count == 0)
            return false;

        IEnumerable<CardModel> selected = await SelectForUpgradeAsync(owner, prefs, candidates);
        List<CardModel> selectedList = selected.ToList();
        if (selectedList.Count == 0)
            return false;

        Traverse.Create(option).Field<IEnumerable<CardModel>>("_selection").Value = selectedList;
        foreach (CardModel picked in selectedList)
            CardCmd.Upgrade(picked, CardPreviewStyle.None);

        await Hook.AfterRestSiteSmith(owner.RunState, owner);
        return true;
    }
}
