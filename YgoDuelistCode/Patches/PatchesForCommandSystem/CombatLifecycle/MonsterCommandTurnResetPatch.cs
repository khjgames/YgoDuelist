using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
public static class MonsterCommandTurnResetPatch
{
    /// <summary>
    /// Per-turn YGO cleanup for the player whose turn started. Stiff/Fatigue removal must also be reconciled at the
    /// "After player turn start" checksum — see <see cref="YgoMonsterCommandChecksumReconcile"/>. Fire-and-forget async
    /// work may still be in flight at snapshot time; blocking with GetResult() can deadlock PowerCmd on the main thread.
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, Player player)
    {
        _ = TaskHelper.RunSafely(PostfixAsync(combatState, choiceContext, player));
    }

    private static async Task PostfixAsync(CombatState combatState, PlayerChoiceContext choiceContext, Player player)
    {
        if (combatState == null || combatState.CurrentSide != CombatSide.Player)
            return;

        var combatPlayer = player;
        if (combatPlayer.PlayerCombatState == null)
            return;

        // Snapshot (ordered by CombatId for MP): hooks can mutate the live pet list during iteration.
        List<Creature> petsAtTurnStart = YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(combatPlayer.PlayerCombatState);
        foreach (Creature pet in petsAtTurnStart)
        {
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is BaseMonsterCard bm)
                bm.FlippedThisTurn = false;
            if (!MonsterCommandRegistry.TryGet(pet, out MonsterCommandState state))
                continue;
            if (state.KeepCommandLockOnNextTurnStart)
            {
                state.KeepCommandLockOnNextTurnStart = false;
                continue;
            }

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
        YgoFushiohRichieSummonGate.ResetForPlayer(combatPlayer);

        BaseTrapCard.ClearSetThisTurnForFacedownSetTrapsInZone(combatPlayer);

        MonsterCommandRegistry.ResetHasAttackedThisTurnForPlayerTurnStart(combatPlayer);

        Ominous_Fortunetelling.RefillAllInSpellTrapZoneForPlayer(combatPlayer);
        await YgoTotalDefenseShogunDeferredBlock.ResolveAtTurnStartAsync(choiceContext, combatPlayer);
        await ResolveTurnStartFieldMonsterAtkGrowthAsync(combatPlayer);

        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRefreshAfterTurnStartIfZoneViewActive(combatPlayer);
    }

    private static async Task ResolveTurnStartFieldMonsterAtkGrowthAsync(Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        List<Creature> petsSnapshot = YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState);
        foreach (Creature pet in petsSnapshot)
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<IYgoTurnStartAtkGrowthFromFieldMonsterAfterCommandReset>(pet) is not IYgoTurnStartAtkGrowthFromFieldMonsterAfterCommandReset hook)
                continue;

            await hook.ApplyTurnStartAtkGrowthAsync(player, pet);
        }
    }
}
