using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Models;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Equip spell target grid: same MP contract as <see cref="CardSelectCmd.FromHand"/> – <see cref="PlayerChoiceResult.FromMutableCombatCards"/>
/// so peers resolve the same <see cref="NetCombatCard"/> targets (index-only sync breaks when pile order differs).
/// Keep equip-target selection plumbing here rather than cloning a custom grid flow into each patch/card.
/// </summary>
public static class EquipSpellGridSelect
{
    private static bool ShouldSelectLocalCard(Player player) =>
        LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;

    public static async Task<IEnumerable<CardModel>> FromSimpleGrid(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cardsIn,
        Player player,
        CardSelectorPrefs prefs)
    {
        if (CombatManager.Instance.IsEnding)
            return Array.Empty<CardModel>();

        List<CardModel> cards = cardsIn.ToList();
        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
            return cards.ToList();

        bool localSelect = ShouldSelectLocalCard(player);
        NetGameType net = RunManager.Instance.NetService.Type;
        bool mpObserver = !localSelect && (net == NetGameType.Host || net == NetGameType.Client);
        IDisposable? expectationScope = null;
        if (mpObserver)
        {
            expectationScope = GridCombatMpExpectation.Push(new GridCombatMpExpectation.Active
            {
                OwnerNetId = player.NetId,
                MinSelect = prefs.Cancelable ? 0 : prefs.MinSelect,
                MaxSelect = prefs.MaxSelect,
                CandidateRowCount = cards.Count,
                AllowCombatCard = true
            });
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);

        try
        {
            await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
            try
            {
                List<CardModel> result;
                if (localSelect)
                {
                    if (CardSelectCmd.Selector != null)
                    {
                        try
                        {
                            result = (await CardSelectCmd.Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect)).ToList();
                        }
                        catch (OperationCanceledException)
                        {
                            SyncCancelIfMp(player, choiceId);
                            throw;
                        }
                    }
                    else
                    {
                        // Same as TributeSummonGridSelect.SelectLocalGridResultsAsync: no CancelAllCardPlay before nested
                        // simple grid during an active combat card play — it can cancel the grid await and desync MP.
                        NSimpleCardSelectScreen screen = NSimpleCardSelectScreen.Create(cards, prefs);
                        NOverlayStack.Instance.Push(screen);
                        try
                        {
                            result = (await screen.CardsSelected()).ToList();
                        }
                        catch (OperationCanceledException)
                        {
                            SyncCancelIfMp(player, choiceId);
                            throw;
                        }
                    }

                    RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                        player,
                        choiceId,
                        PlayerChoiceResult.FromMutableCombatCards(result));
                }
                else
                {
                    PlayerChoiceResult remoteResult =
                        await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId);
                    if (remoteResult.ChoiceType == PlayerChoiceType.CombatCard)
                    {
                        result = remoteResult.AsCombatCards().ToList();
                    }
                    else if (remoteResult.ChoiceType == PlayerChoiceType.Index)
                    {
                        List<int> remoteIndexes = remoteResult.AsIndexes().ToList();
                        if (remoteIndexes.Count < prefs.MinSelect || remoteIndexes.Count > prefs.MaxSelect)
                        {
                            throw new InvalidOperationException(
                                $"[YgoDuelist][MP][Equip] Remote Index count {remoteIndexes.Count} outside [{prefs.MinSelect},{prefs.MaxSelect}] choiceId={choiceId} ownerNet={player.NetId}.");
                        }

                        GD.PrintErr(
                            $"[YgoDuelist][MP][Equip] Remote choice was Index choiceId={choiceId} ownerNet={player.NetId} indexes=[{string.Join(",", remoteIndexes)}] — applying into grid snapshot.");
                        if (remoteIndexes.Any(i => i < 0 || i >= cards.Count))
                            throw new InvalidOperationException("[YgoDuelist][MP][Equip] Remote indexes out of range.");
                        result = remoteIndexes.Select(i => cards[i]).ToList();
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"[YgoDuelist][MP][Equip] Unexpected PlayerChoiceType {remoteResult.ChoiceType} choiceId={choiceId}");
                    }
                }

                return result;
            }
            finally
            {
                await context.SignalPlayerChoiceEnded();
            }
        }
        finally
        {
            expectationScope?.Dispose();
        }
    }

    private static void SyncCancelIfMp(Player player, uint choiceId)
    {
        if (!LocalContext.IsMe(player))
            return;
        if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            return;

        GD.Print($"[YgoDuelist][MP][Equip] Grid cancel → empty combat cards choiceId={choiceId} ownerNet={player.NetId}");
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
            player,
            choiceId,
            PlayerChoiceResult.FromMutableCombatCards(new List<CardModel>()));
    }
}
