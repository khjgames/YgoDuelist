using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>Run before other <see cref="PlayCardAction"/> prefixes that call <see cref="NetCombatCard.ToCardModel"/> — it throws when the combat id only exists on the host.</summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(900)]
public static class PlayCardFromSpellTrapZonePatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        var player = __instance.Player;
        if (player == null)
            return true;

        CardModel? card = YgoSpellTrapPlayCardMpResolver.ResolveSpellTrapPlayCard(__instance);
        if (card == null)
            return true;

        // Harmony aborts later Prefix patches when one returns false. Same card set as YgoPlayCardQueueDeferral.
        if (YgoPlayCardQueueDeferral.SpellTrapZonePlayRequiresVanillaExecuteAction(card))
            return true;

        // Priority 900 runs before PlayCardActionPrePlayCancelableGridPatch (790). For IYgoPrePlayCancelableGridSelection,
        // this path can complete on observers without OnPlay (ResolveSpellTrapPlayCard null / early exit) while the host
        // falls through to PrePlayGrid — checksum ID divergence (e.g. Raigeki Break). Let PrePlayGrid own the full flow.
        if (card is IYgoPrePlayCancelableGridSelection)
        {
            if (YgoMpDiagnostics.IsMultiplayer)
            {
                GD.Print(
                    $"[YgoDuelist][MP][SpellTrapPlay] defer to PrePlayGrid owner={player.NetId} card={card.Id?.Entry}");
            }

            return true;
        }

        CardPile? pile = card.Pile;
        CardPile? zonePile = YgoPlayerPiles.SpellTrapZone(player);
        if (pile == null || zonePile == null || !ReferenceEquals(pile, zonePile))
            return true;

        __result = ExecutePlayFromSpellTrapZoneAsync(__instance);
        return false;
    }

    private static async Task ExecutePlayFromSpellTrapZoneAsync(PlayCardAction action)
    {
        YgoSpellTrapPlayCardMpResolver.TryRebindNetCombatCardIfSpellTrapZone(action);
        CardModel? card = YgoSpellTrapPlayCardMpResolver.ResolveSpellTrapPlayCard(action);
        if (card == null)
            return;

        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
        Creature? playerCreature = action.Player.Creature;
        if (playerCreature?.CombatState is not CombatState combatState)
            return;
        Creature? target = await combatState.GetCreatureAsync(action.TargetId, 10.0);

        if (card is BaseSpellCard bs)
        {
            target = await bs.TryResolveSpellTrapZonePlayTargetAsync(action.Player, target, cancelable: true);
            if (target == null && bs.CancelSpellTrapZonePlayWhenUnresolvedTargetAfterResolve)
            {
                action.Cancel();
                return;
            }
        }

        bool needsTarget = card.TargetType == TargetType.AnyEnemy || card.TargetType == TargetType.AnyAlly;
        if (needsTarget && target == null)
        {
            action.Cancel();
            return;
        }

        bool observingOtherPlayer = action.Player != null && !LocalContext.IsMe(action.Player);
        if (!observingOtherPlayer
            && (!card.CanPlay(out _, out _) || !IsValidTargetForSpellTrapZonePlay(card, target)))
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

        YgoSpellTrapZoneAfterPlayUi.ScheduleCleanup(action.Player, card);
    }

    /// <summary>
    /// Vanilla <see cref="CardModel.IsValidTarget"/> returns false for non-null targets when
    /// <see cref="TargetType"/> is not AnyEnemy/AnyAlly. Field-monster spells use <see cref="TargetType.None"/>
    /// so <see cref="CardModel.CanPlay"/> is not blocked by the AnyAlly "2+ PlayerCreatures" rule
    /// (duel pets are often not in that list); we still require a valid field monster here.
    /// </summary>
    private static bool IsValidTargetForSpellTrapZonePlay(CardModel card, Creature? target)
    {
        if (card is BaseSpellCard bs)
            return bs.IsValidTargetForSpellTrapZonePlay(target);
        return card.IsValidTarget(target);
    }
}
