using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
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
    private static readonly LocString ActivateFlipEffectsPrompt =
        new("cards", "YGODUELIST-FLIP_EFFECT.activate.selection");

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

        var promptCandidates = new List<AbstractMonsterCard>();
        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!FlipFaceDownOnPlayerEnemyAttackHelpers.TryGetEligibleFaceDownSourceCard(pet, out AbstractMonsterCard? card))
                continue;

            if (card is not IMonsterFlipEffect)
            {
                FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpNow(card, choiceContext);
                continue;
            }

            bool askPrompt = card is not BaseMonsterCard bm || bm.AskSelectFlip;
            if (!askPrompt)
            {
                FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpNow(card, choiceContext);
                continue;
            }

            promptCandidates.Add(card);
        }

        if (promptCandidates.Count == 0)
            return;

        TaskHelper.RunSafely(SelectFlipEffectsToActivateAsync(choiceContext, player, promptCandidates));
    }

    private static async Task SelectFlipEffectsToActivateAsync(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        IReadOnlyList<AbstractMonsterCard> promptCandidates)
    {
        var prefs = new CardSelectorPrefs(ActivateFlipEffectsPrompt, 0, promptCandidates.Count)
        {
            Cancelable = true
        };

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(choiceContext, promptCandidates, player, prefs);
        foreach (CardModel cardModel in selected)
        {
            if (cardModel is not AbstractMonsterCard selectedCard || !selectedCard.FaceDown)
                continue;
            FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpNow(selectedCard, choiceContext);
        }
    }
}

internal static class FlipFaceDownOnPlayerEnemyAttackHelpers
{
    public static bool TryGetEligibleFaceDownSourceCard(Creature pet, out AbstractMonsterCard? card)
    {
        card = null;
        if (!pet.IsAlive)
            return false;
        if (!MonsterCommandRegistry.PetHasUsedAnyCommandSlotThisTurn(pet))
            return false;
        if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not AbstractMonsterCard sourceCard)
            return false;
        if (!sourceCard.FaceDown || !sourceCard.IsMutable)
            return false;
        card = sourceCard;
        return true;
    }

    public static void ForceFlipFaceUpNow(AbstractMonsterCard card, PlayerChoiceContext choiceContext)
    {
        bool wasFaceDown = card.FaceDown;
        card.FaceDown = false;
        card.UpdateFaceDownKeywordFromBool();
        YgoMonsterFlipEffectRunner.ScheduleIfFlippedOnField(card, wasFaceDown, choiceContext);
    }
}
