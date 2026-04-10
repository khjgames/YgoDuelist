using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// For multiplayer, vanilla <see cref="PlayCardAction.ExecuteAction"/> does not run the same awaited play body on the
/// machine where <see cref="LocalContext.IsMe(Player)"/> is false for <see cref="PlayCardAction.Player"/> (the peer who
/// owns the card). The host then fully resolves e.g. <see cref="NormalMonsterCard.OnPlay"/> (summon, combat damage)
/// while an observing client completes the action in the same frame — post-action checksums diverge (card still in hand,
/// enemy HP unchanged on client). Tribute / fusion / spell-trap / option-pile patches already mirror the vanilla body
/// for every peer; this covers the default <see cref="NormalMonsterCard"/> play from <see cref="PileType.Hand"/> only.
/// <para/>
/// Must not handle <see cref="TributeSummonSelection.IsHandTributeDuelNormalSummonPlay"/>: this prefix runs at higher
/// priority than <see cref="PlayCardActionTributeSelectionPatch"/>; stealing remote tribute summons would skip tribute
/// sync and leave the card in hand on observers (checksum ID divergence after e.g. Blue-Eyes tribute).
/// <para/>
/// Remote peers must not gate on <see cref="CardModel.CanPlay"/> / hook state: replicated energy or hooks can disagree
/// with the authoritative host for the same tick, so the mirrored body skips those checks (the enqueueing player already passed them).
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(100)]
public static class PlayCardActionRemoteHandDuelMonsterFixPatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    private static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        try
        {
            if (LocalContext.IsMe(__instance.Player))
                return true;
        }
        catch
        {
            return true;
        }

        CardModel? card = __instance.NetCombatCard.ToCardModelOrNull();
        if (card is not NormalMonsterCard || card.Pile?.Type != PileType.Hand)
            return true;

        if (TributeSummonSelection.IsHandTributeDuelNormalSummonPlay(__instance))
            return true;

        __result = ExecuteVanillaPlayCardActionBodyAsync(__instance);
        return false;
    }

    /// <summary>Same steps as the mirrored vanilla body in <c>PlayCardActionTributeSelectionPatch</c>.</summary>
    private static async Task ExecuteVanillaPlayCardActionBodyAsync(PlayCardAction action)
    {
        CardModel? card = action.NetCombatCard.ToCardModel();
        if (card == null || action.Player?.Creature?.CombatState == null)
            return;

        GD.Print(
            $"[YgoDuelist][MP][PlayCard] Remote peer: mirrored vanilla ExecuteAction body (owner={action.Player.NetId} card={card.Id?.Entry})");

        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
        Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);
        CardPile? pile = card.Pile;
        if (pile == null || pile.Type != PileType.Hand)
        {
            GD.PrintErr(
                $"[YgoDuelist][MP][PlayCard] Remote mirror abort: card not in Hand (pile={(pile == null ? "null" : pile.Type.ToString())}) owner={action.Player.NetId} card={card.Id?.Entry}");
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

        // Do not call CanPlay/IsValidTarget here: for the observing peer, replicated resources/hooks can disagree with
        // the owner for one frame while the action is still authoritative (vanilla would not run this body at all on remotes).
        string? targetLabel = target == null ? null : (target.IsPlayer ? $"Player {target.Player.NetId}" : target.Name);
        string targetPart = targetLabel != null
            ? $"targeting {targetLabel} (combatId {target?.CombatId})"
            : "no target";
        Log.Info($"Player {card.Owner.NetId} playing card {card.Id?.Entry} ({targetPart}) [YgoDuelist remote mirror, skipped CanPlay]");

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
