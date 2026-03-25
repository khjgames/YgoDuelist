using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When the player character is hit by an enemy attack (including fully blocked damage),
/// face-down duel monsters that have already used their Command this turn are flipped face up.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
public static class FlipFaceDownMonstersOnPlayerEnemyAttackPatch
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

        if (dealer == null || dealer.Side != CombatSide.Enemy)
            return;

        // Main player character only (not duel monster pets).
        if (!target.IsPlayer || target.IsPet)
            return;

        if (target.Side != CombatSide.Player)
            return;

        // Combat attacks / monster moves use Move; excludes typical passive HP loss (e.g. poison).
        if (!props.HasFlag(ValueProp.Move))
            return;

        var player = target.Player;
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive)
                continue;

            if (!MonsterCommandRegistry.GetOrCreate(pet).HasUsedCommandThisTurn)
                continue;

            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not AbstractMonsterCard card)
                continue;

            if (!card.FaceDown)
                continue;

            if (!card.IsMutable)
                continue;

            bool wasFaceDown = card.FaceDown;
            card.FaceDown = false;
            card.UpdateFaceDownKeywordFromBool();
            YgoMonsterFlipEffectRunner.ScheduleIfFlippedOnField(card, wasFaceDown, choiceContext);
        }
    }
}
