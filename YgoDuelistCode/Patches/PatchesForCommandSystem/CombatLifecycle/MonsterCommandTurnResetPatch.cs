using System.Threading.Tasks;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
public static class MonsterCommandTurnResetPatch
{
    /// <summary>
    /// Per-turn YGO cleanup for the player whose turn started. Stiff/Fatigue removal must also be reconciled at the
    /// "After player turn start" checksum — see <see cref="YgoMonsterCommandChecksumReconcile"/> (async void here cannot
    /// complete before that snapshot; blocking with GetResult() can deadlock PowerCmd on the main thread).
    /// </summary>
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
            if (!MonsterCommandRegistry.TryGet(pet, out _))
                continue;

            bool hadStiffOrFatigue = pet.HasPower<StiffPower>() || pet.HasPower<FatiguePower>();
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(pet, false, combatPlayer.Creature, null);
            if (hadStiffOrFatigue)
            {
                GD.Print(
                    $"[YgoDuelist][MP][MonsterCommandTurnReset] hook cleared command lock powers petCombatId={pet.CombatId} netId={combatPlayer.NetId}");
            }
        }

        NormalSummonTracker.ResetForPlayer(combatPlayer);
        ColdWaveSpellTrapLockGate.ResetSpellTrapUsageForPlayerTurnStart(combatPlayer);
        LegionFiendJesterSpellcasterConduit.ResetForPlayer(combatPlayer);
        ReactorSlimeSummonGate.ResetForPlayer(combatPlayer);

        BaseTrapCard.ClearSetThisTurnForFacedownSetTrapsInZone(combatPlayer);

        Ominous_Fortunetelling.RefillAllInSpellTrapZoneForPlayer(combatPlayer);

        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRefreshAfterTurnStartIfZoneViewActive(combatPlayer);

        await Task.CompletedTask;
    }
}
