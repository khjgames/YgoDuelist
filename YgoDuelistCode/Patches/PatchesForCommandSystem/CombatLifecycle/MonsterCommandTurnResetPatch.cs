using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
public static class MonsterCommandTurnResetPatch
{
    // Match Hook.AfterPlayerTurnStart signature and use async void, like Oddmelt.
    [HarmonyPostfix]
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, Player player)
    {
        if (combatState == null || combatState.CurrentSide != CombatSide.Player)
            return;

        var combatPlayer = player;
        if (combatPlayer.PlayerCombatState == null)
            return;

        foreach (Creature pet in combatPlayer.PlayerCombatState.Pets)
        {
            if (MonsterCommandRegistry.TryGet(pet, out _))
            {
                await MonsterCommandRegistry.SetHasUsedCommandThisTurn(pet, false, combatPlayer.Creature, null);
            }
        }

        NormalSummonTracker.ResetForPlayer(combatPlayer);
        LegionFiendJesterSpellcasterConduit.ResetForPlayer(combatPlayer);

        BaseTrapCard.ClearSetThisTurnForFacedownSetTrapsInZone(combatPlayer);

        Ominous_Fortunetelling.RefillAllInSpellTrapZoneForPlayer(combatPlayer);

        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRefreshAfterTurnStartIfZoneViewActive(combatPlayer);

        await Task.CompletedTask;
    }
}
