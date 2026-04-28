using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Services;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Duel monsters destroyed by enemy combat hits (Move damage) — used for YGO "destroyed by battle" style triggers.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
public static class MarkDuelMonsterDestroyedByBattleDamagePatch
{
    [HarmonyPostfix]
    public static void Postfix(
        PlayerChoiceContext choiceContext,
        IRunState runState,
        CombatState? combatState,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (combatState == null || CombatManager.Instance?.IsInProgress != true)
            return;

        if (!target.IsPet || dealer == null || dealer.Side != CombatSide.Enemy)
            return;

        bool isBattle =
            props.HasFlag(ValueProp.Move)
            || target.HasPower<DieForYouPower>();

        if (!isBattle)
            return;

        if (!result.WasTargetKilled)
            return;

        MonsterCommandState state = MonsterCommandRegistry.GetOrCreate(target);
        state.DestroyedByEnemyBattleDamage = true;
        state.BattleDamageKillerEnemy = dealer;
    }
}
