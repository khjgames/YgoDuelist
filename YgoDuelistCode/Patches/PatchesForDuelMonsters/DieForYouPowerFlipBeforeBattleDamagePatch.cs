using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When the player is hit by an enemy powered <see cref="ValueProp.Move"/> attack, each
/// <see cref="DieForYouPower"/> pet flips (and may resolve FLIP effects) during the awaited
/// <see cref="AbstractModel.BeforeDamageReceived"/> chain — before spill damage is applied. Lethal pet
/// kills skip <see cref="MegaCrit.Sts2.Core.Hooks.Hook.AfterDamageReceived"/>, so this cannot run there.
/// </summary>
[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.BeforeDamageReceived))]
public static class DieForYouPowerFlipBeforeBattleDamagePatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AbstractModel __instance,
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref Task __result)
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        if (__instance is not DieForYouPower dieForYou)
            return;

        if (dieForYou.Owner is not Creature pet || !pet.IsPet || pet.Side != CombatSide.Player)
            return;

        if (dealer == null || dealer.Side != CombatSide.Enemy)
            return;

        if (!props.HasFlag(ValueProp.Move) || !props.IsPoweredAttack())
            return;

        bool playerIsOriginalTarget =
            target.IsPlayer && !target.IsPet && target.Side == CombatSide.Player;
        bool petIsOriginalTarget = ReferenceEquals(target, pet);
        if (!playerIsOriginalTarget && !petIsOriginalTarget)
            return;

        if (playerIsOriginalTarget && pet.PetOwner?.Creature != target)
            return;

        __result = RunAfterPriorAsync(
            __result,
            () => FlipFaceDownOnPlayerEnemyAttackHelpers.ResolveDieForYouPetFaceDownFlipBeforeBattleDamageAsync(
                choiceContext,
                pet));
    }

    private static async Task RunAfterPriorAsync(Task prior, Func<Task> next)
    {
        await prior.ConfigureAwait(true);
        await next().ConfigureAwait(true);
    }
}
