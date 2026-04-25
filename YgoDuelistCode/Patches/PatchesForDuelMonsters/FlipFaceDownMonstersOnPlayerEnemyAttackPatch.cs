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

    private sealed class PendingFlipPromptState
    {
        public readonly List<AbstractMonsterCard> Pending = new();
        public bool DrainScheduled;
        public bool PromptActive;
    }

    private static readonly Dictionary<ulong, PendingFlipPromptState> PendingByPlayerNetId = new();

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
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
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

            if (!FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpWithoutActivatingEffectNow(card, choiceContext))
                continue;

            promptCandidates.Add(card);
        }

        if (promptCandidates.Count == 0)
            return;

        EnqueueFlipPromptCandidates(choiceContext, player, promptCandidates);
    }

    private static void EnqueueFlipPromptCandidates(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        IReadOnlyList<AbstractMonsterCard> promptCandidates)
    {
        ulong key = player.NetId;
        if (!PendingByPlayerNetId.TryGetValue(key, out PendingFlipPromptState? state))
        {
            state = new PendingFlipPromptState();
            PendingByPlayerNetId[key] = state;
        }

        foreach (AbstractMonsterCard candidate in promptCandidates)
        {
            if (state.Pending.Any(c => ReferenceEquals(c, candidate)))
                continue;
            state.Pending.Add(candidate);
        }

        Godot.GD.Print(
            $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] queued prompt candidates owner={player.NetId} added={promptCandidates.Count} pending={state.Pending.Count} scheduled={state.DrainScheduled} active={state.PromptActive}");

        if (state.DrainScheduled || state.PromptActive)
            return;

        state.DrainScheduled = true;
        _ = TaskHelper.RunSafely(DrainFlipPromptCandidatesAsync(choiceContext, player));
    }

    private static async Task DrainFlipPromptCandidatesAsync(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        // Let same-frame multi-hit hooks enqueue into one combined prompt before opening the grid.
        await WaitOneProcessFrameAsync();

        if (!PendingByPlayerNetId.TryGetValue(player.NetId, out PendingFlipPromptState? state))
            return;

        List<AbstractMonsterCard> promptCandidates = YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(state.Pending)
            .OfType<AbstractMonsterCard>()
            .Distinct()
            .ToList();
        state.Pending.Clear();
        state.DrainScheduled = false;
        if (promptCandidates.Count == 0 || CombatManager.Instance?.IsInProgress != true)
        {
            PendingByPlayerNetId.Remove(player.NetId);
            return;
        }

        state.PromptActive = true;
        try
        {
            await SelectFlipEffectsToActivateAsync(choiceContext, player, promptCandidates);
        }
        finally
        {
            state.PromptActive = false;
        }

        if (state.Pending.Count == 0)
            PendingByPlayerNetId.Remove(player.NetId);
        else if (!state.DrainScheduled)
        {
            state.DrainScheduled = true;
            _ = TaskHelper.RunSafely(DrainFlipPromptCandidatesAsync(choiceContext, player));
        }
    }

    private static async Task WaitOneProcessFrameAsync()
    {
        if (Godot.Engine.GetMainLoop() is Godot.SceneTree tree)
            await tree.ToSignal(tree, Godot.SceneTree.SignalName.ProcessFrame);
        else
            await Task.Yield();
    }

    private static async Task SelectFlipEffectsToActivateAsync(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        IReadOnlyList<AbstractMonsterCard> promptCandidates)
    {
        var prefs = new CardSelectorPrefs(ActivateFlipEffectsPrompt, 1, promptCandidates.Count)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        Godot.GD.Print(
            $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] showing combined activate prompt owner={player.NetId} candidates={promptCandidates.Count} min=1 cancelable=true");

        List<AbstractMonsterCard> selected = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            prefs,
            () => YgoMpCombatOrder.CardsSnapshotOrderedForMp(promptCandidates).OfType<AbstractMonsterCard>().ToList(),
            promptCandidates.Count);
        foreach (AbstractMonsterCard selectedCard in selected)
        {
            if (selectedCard is not IMonsterFlipEffect flip)
                continue;

            await YgoMonsterFlipEffectRunner.RunFlipEffectAsync(flip, YgoChoiceContexts.Blocking(choiceContext), selectedCard);
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
        if (DuelMonsterFieldRegistry.GetSourceMonster<AbstractMonsterCard>(pet) is not AbstractMonsterCard sourceCard)
            return false;
        if (!sourceCard.FaceDown || !sourceCard.IsMutable)
            return false;
        card = sourceCard;
        return true;
    }

    public static void ForceFlipFaceUpNow(AbstractMonsterCard card, PlayerChoiceContext choiceContext)
    {
        bool flipped = ForceFlipFaceUpWithoutActivatingEffectNow(card, choiceContext);
        if (!flipped || card is not IMonsterFlipEffect flip)
            return;

        TaskHelper.RunSafely(YgoMonsterFlipEffectRunner.RunFlipEffectAsync(
            flip,
            YgoChoiceContexts.Blocking(choiceContext),
            card));
    }

    public static bool ForceFlipFaceUpWithoutActivatingEffectNow(AbstractMonsterCard card, PlayerChoiceContext choiceContext)
    {
        bool wasFaceDown = card.FaceDown;
        card.FaceDown = false;
        card.UpdateFaceDownKeywordFromBool();
        bool marked = YgoMonsterFlipEffectRunner.MarkFlippedFaceUpOnField(card, wasFaceDown);
        Godot.GD.Print(
            $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] flip face-up source={card.Id?.Entry} wasFaceDown={wasFaceDown} marked={marked}");
        return marked;
    }
}
