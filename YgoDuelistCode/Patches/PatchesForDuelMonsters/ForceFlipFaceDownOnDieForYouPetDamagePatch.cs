using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// If Die For You redirects spill damage to the pet, that pet's set monster is forced face-up.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
public static class ForceFlipFaceDownOnDieForYouPetDamagePatch
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

        if (!target.IsPet || target.Side != CombatSide.Player)
            return;

        if (dealer == null || dealer.Side != CombatSide.Enemy)
            return;

        if (!props.HasFlag(ValueProp.Move) || !props.IsPoweredAttack())
            return;

        if (result.UnblockedDamage <= 0 || !target.HasPower<DieForYouPower>())
            return;

        if (!FlipFaceDownOnPlayerEnemyAttackHelpers.TryGetEligibleFaceDownSourceCard(
                target,
                requireAlive: false,
                requireUsedCommandThisTurn: false,
                out var card))
            return;

        FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpNow(card, choiceContext);
    }
}
