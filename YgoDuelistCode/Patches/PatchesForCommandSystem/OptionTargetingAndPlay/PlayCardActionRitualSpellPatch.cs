using System.Reflection;
using System.Threading.Tasks;
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
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Before spending resources, ritual spells open grids for ritual monster + materials; cancel aborts the play.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
public static class PlayCardActionRitualSpellPatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        try
        {
            if (!LocalContext.IsMe(__instance.Player))
                return true;
        }
        catch
        {
            return true;
        }

        var card = __instance.NetCombatCard.ToCardModel();
        if (card is not RitualSpellCard)
            return true;

        bool fromHand = card.Pile?.Type == PileType.Hand;
        bool fromSpellTrapZone = card.Pile?.Type == SpellTrapZonePile.CustomType;
        if (!fromHand && !fromSpellTrapZone)
            return true;

        __result = ExecuteWithRitualAsync(__instance);
        return false;
    }

    private static async Task ExecuteWithRitualAsync(PlayCardAction action)
    {
        CardModel? card = action.NetCombatCard.ToCardModel();
        if (card is not RitualSpellCard ritualSpell)
            return;

        if (!await RitualSummonSelection.TrySelectRitualResolutionAsync(action.Player, ritualSpell))
        {
            action.Cancel();
            return;
        }

        try
        {
            RitualSummonSelection.BeginCompletingRitualSpellPlay();
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            RitualSummonSelection.EndCompletingRitualSpellPlay();
            RitualSpellPlayPayload.ClearForCard(card);
        }
    }

    /// <summary>Mirrors <see cref="PlayCardAction.ExecuteAction"/> after ritual resolution.</summary>
    private static async Task ExecuteVanillaPlayCardActionBody(PlayCardAction action)
    {
        CardModel? card = action.NetCombatCard.ToCardModel();
        if (card == null)
            return;

        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
        Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);

        bool playedFromSpellTrapZone = card.Pile?.Type == SpellTrapZonePile.CustomType;

        CardPile? pile = card.Pile;
        if (pile == null || (pile.Type != PileType.Hand && pile.Type != SpellTrapZonePile.CustomType))
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

        if (playedFromSpellTrapZone)
            YgoSpellTrapZoneAfterPlayUi.ScheduleCleanup(action.Player, card);
    }
}
