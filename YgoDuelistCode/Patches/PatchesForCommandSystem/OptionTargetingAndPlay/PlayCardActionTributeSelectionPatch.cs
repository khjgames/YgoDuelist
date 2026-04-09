using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Before spending resources, tribute monsters open a grid to pick field materials; cancel aborts the play.
/// Must run for <b>every</b> peer in MP: <see cref="MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromSimpleGrid"/> uses
/// <c>ShouldSelectLocalCard</c> / <c>WaitForRemoteChoice</c> so only the summoning player sees the grid; remotes block until
/// the owner syncs confirm or cancel. Gating on <see cref="LocalContext.IsMe"/> let remotes execute vanilla
/// <see cref="PlayCardAction"/> immediately (no tribute wait), causing checksum divergence and broken cancel flows.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(850)]
public static class PlayCardActionTributeSelectionPatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!TributeSummonSelection.IsHandTributeDuelNormalSummonPlay(__instance))
            return true;

        __result = ExecuteWithTributeSelectionAsync(__instance);
        return false;
    }

    private static async Task ExecuteWithTributeSelectionAsync(PlayCardAction action)
    {
        CardModel? card = null;
        try
        {
            card = action.NetCombatCard.ToCardModel();
            if (card is not NormalMonsterCard nmc || nmc.TributeReleaseCount <= 0)
                return;

            var resolution = await TributeSummonSelection.SelectTributesForNormalSummonAsync(action.Player, nmc);
            if (resolution == null)
            {
                action.Cancel();
                return;
            }

            GD.Print(
                $"[YgoDuelist][MP][Tribute] SetPending netCardIdx={action.NetCombatCard.CombatCardIndex} owner={action.Player.NetId} pets={resolution.Pets.Count} card={card.Id?.Entry}");
            TributeSummonPlayPayload.SetPending(action.Player.NetId, action.NetCombatCard.CombatCardIndex, resolution);
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            if (card != null)
                TributeSummonPlayPayload.ClearForKey(action.Player.NetId, action.NetCombatCard.CombatCardIndex);
        }
    }

    /// <summary>Mirrors <see cref="PlayCardAction.ExecuteAction"/> after tribute selection is resolved.</summary>
    private static async Task ExecuteVanillaPlayCardActionBody(PlayCardAction action)
    {
        CardModel? card = action.NetCombatCard.ToCardModel();
        if (card == null)
            return;

        GD.Print(
            $"[YgoDuelist][Queue][Defer] Execute body: UpdateCardBeforeExecution after tribute confirm (player {action.Player.NetId}, card {card.Id?.Entry})");
        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
        Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);
        CardPile? pile = card.Pile;
        if (pile == null || pile.Type != PileType.Hand)
        {
            NCardPlayQueue.Instance?.RemoveCardFromQueueForCancellation(action);
            return;
        }

        bool warnMissingTarget = target == null;
        if (warnMissingTarget)
        {
            TargetType targetType = card.TargetType;
            warnMissingTarget = targetType == TargetType.AnyEnemy || targetType == TargetType.AnyAlly;
        }

        if (warnMissingTarget)
        {
            Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");
        }

        if (!card.CanPlay(out _, out _) || !card.IsValidTarget(target))
        {
            action.Cancel();
            return;
        }

        (int energySpent, int starsSpent) = await card.SpendResources();
        var resources = new ResourceInfo
        {
            EnergySpent = energySpent,
            EnergyValue = energySpent,
            StarsSpent = starsSpent,
            StarValue = starsSpent
        };

        var context = new GameActionPlayerChoiceContext(action);
        PlayerChoiceContextProp?.SetValue(action, context);
        await card.OnPlayWrapper(context, target, isAutoPlay: false, resources);
    }
}
