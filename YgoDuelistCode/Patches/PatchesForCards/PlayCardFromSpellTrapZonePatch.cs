using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
public static class PlayCardFromSpellTrapZonePatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        var player = __instance.Player;
        if (player == null)
            return true;

        CardModel? card = __instance.NetCombatCard.ToCardModel();
        if (card == null)
            return true;

        // Harmony aborts later Prefix patches when one returns false. Fusion/ritual use their own
        // PlayCardAction.ExecuteAction handlers (selection grids, then vanilla body); they must run instead of this path.
        if (card is FusionSpellCard or RitualSpellCard)
            return true;

        if (card is BaseEquipSpellCard)
            return true;

        CardPile? pile = card.Pile;
        CardPile? zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (pile == null || zonePile == null || !ReferenceEquals(pile, zonePile))
            return true;

        __result = ExecutePlayFromSpellTrapZoneAsync(__instance);
        return false;
    }

    private static async Task ExecutePlayFromSpellTrapZoneAsync(PlayCardAction action)
    {
        CardModel? card = action.NetCombatCard.ToCardModel();
        if (card == null)
            return;

        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
        Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);

        // Burst Stream (played from set Spell/Trap zone) should silently ask for Blue-Eyes selection.
        if (card is Burst_Stream_of_Destruction && target == null)
        {
            target = await Burst_Stream_of_Destruction.PickBlueEyesOnFieldAsync(action.Player, cancelable: true);
            if (target == null)
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

        YgoSpellTrapZoneAfterPlayUi.ScheduleCleanup(action.Player, card);
    }
}
