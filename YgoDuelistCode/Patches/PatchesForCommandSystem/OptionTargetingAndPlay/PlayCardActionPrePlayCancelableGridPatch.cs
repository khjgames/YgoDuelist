using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Runs cancelable confirm grid before <see cref="CardModel.SpendResources"/> for <see cref="IYgoPrePlayCancelableGridSelection"/>.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(790)]
public static class PlayCardActionPrePlayCancelableGridPatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    private static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        CardModel? card = __instance.NetCombatCard.ToCardModel();
        if (card is not IYgoPrePlayCancelableGridSelection preplay)
            return true;

        if (!TryResolveAllowedPrePlayPlaySource(__instance.Player, card, out string sourceTag))
            return true;

        if (sourceTag.EndsWith("_infer", StringComparison.Ordinal))
            GD.PrintErr(
                $"[YgoDuelist][MP][PrePlayGrid] pile_infer source={sourceTag} owner={__instance.Player?.NetId} card={card.Id?.Entry} " +
                $"pileType={(int)(card.Pile?.Type ?? 0)} (MP observer / stale Pile reference)");

        __result = ExecuteWithPrePlayGridAsync(__instance, card, preplay);
        return false;
    }

    /// <summary>
    /// Pre-play grid must run on <b>every</b> peer for lockstep. Remote <see cref="CardModel.Pile"/> is often null or not
    /// reference-equal to the zone pile even when the card is in hand / spell-trap / option row — then we would skip this
    /// patch, run vanilla <see cref="PlayCardAction"/> (no grid, no <see cref="CardModel.OnPlayWrapper"/>), and checksum diverges.
    /// </summary>
    private static bool TryResolveAllowedPrePlayPlaySource(Player? player, CardModel card, out string sourceTag)
    {
        sourceTag = "";
        if (player == null)
            return false;

        if (card.Pile?.Type == PileType.Hand)
        {
            sourceTag = "hand";
            return true;
        }

        if (card.Pile?.Type == SpellTrapZonePile.CustomType)
        {
            sourceTag = "spell_trap_zone";
            return true;
        }

        if (card.Pile?.Type == YgoCardOptionPile.CustomType)
        {
            sourceTag = "option_pile";
            return true;
        }

        if (RunManager.Instance?.NetService.Type == NetGameType.Singleplayer)
            return false;

        if (PileType.Hand.GetPile(player)?.Cards.Contains(card) == true)
        {
            sourceTag = "hand_infer";
            return true;
        }

        if (SpellTrapZonePile.CustomType.GetPile(player)?.Cards.Contains(card) == true)
        {
            sourceTag = "spell_trap_zone_infer";
            return true;
        }

        if (YgoCardOptionPile.CustomType.GetPile(player)?.Cards.Contains(card) == true)
        {
            sourceTag = "option_pile_infer";
            return true;
        }

        return false;
    }

    private static async Task ExecuteWithPrePlayGridAsync(
        PlayCardAction action,
        CardModel card,
        IYgoPrePlayCancelableGridSelection preplay)
    {
        bool localOwner;
        try
        {
            localOwner = LocalContext.IsMe(action.Player);
        }
        catch
        {
            localOwner = false;
        }

        GD.Print(
            $"[YgoDuelist][MP][PrePlayGrid] begin owner={action.Player.NetId} localOwner={localOwner} card={card.Id?.Entry}");
        try
        {
            if (!await preplay.TryPreparePrePlayCancelableGridAsync(action.Player, card))
            {
                GD.Print(
                    $"[YgoDuelist][MP][PrePlayGrid] canceled_before_play owner={action.Player.NetId} localOwner={localOwner} card={card.Id?.Entry}");
                action.Cancel();
                return;
            }

            await ExecuteVanillaPlayCardActionBody(action);
            GD.Print(
                $"[YgoDuelist][MP][PrePlayGrid] execute_complete owner={action.Player.NetId} localOwner={localOwner} card={card.Id?.Entry}");
        }
        finally
        {
            YgoPrePlaySelectedCardPayload.ClearForCard(card);
            YgoPrePlayOptionIdPayload.ClearForCard(card);
        }
    }

    private static async Task ExecuteVanillaPlayCardActionBody(PlayCardAction action)
    {
        CardModel? card = action.NetCombatCard.ToCardModel();
        if (card == null)
            return;

        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
        Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);

        bool pileOk = TryResolveAllowedPrePlayPlaySource(action.Player, card, out string sourceTagBody);
        if (!pileOk)
        {
            GD.PrintErr(
                $"[YgoDuelist][MP][PrePlayGrid] execute_abort_no_pile owner={action.Player?.NetId} card={card.Id?.Entry} pileNull={card.Pile == null}");
            NCardPlayQueue.Instance?.RemoveCardFromQueueForCancellation(action);
            return;
        }

        if (sourceTagBody.EndsWith("_infer", StringComparison.Ordinal))
            GD.PrintErr(
                $"[YgoDuelist][MP][PrePlayGrid] execute_pile_infer source={sourceTagBody} owner={action.Player?.NetId} card={card.Id?.Entry}");

        bool warnMissingTarget = target == null;
        if (warnMissingTarget)
        {
            TargetType targetType = card.TargetType;
            warnMissingTarget = targetType == TargetType.AnyEnemy || targetType == TargetType.AnyAlly;
        }

        if (warnMissingTarget)
            Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");
        bool observingOtherPlayer = action.Player != null && !LocalContext.IsMe(action.Player);
        if (observingOtherPlayer)
        {
            GD.Print(
                $"[YgoDuelist][MP][PrePlayGrid] observing_remote_skip_playability_checks owner={action.Player.NetId} card={card.Id?.Entry}");
        }

        if (!observingOtherPlayer && (!card.CanPlay(out _, out _) || !card.IsValidTarget(target)))
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
