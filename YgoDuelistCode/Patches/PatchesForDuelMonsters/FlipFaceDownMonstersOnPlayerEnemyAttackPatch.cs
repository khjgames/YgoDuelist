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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When the player character is hit by an enemy <see cref="MegaCrit.Sts2.Core.ValueProps.ValueProp.Move"/> attack
/// (including fully blocked HP loss—hook still runs), face-down duel monsters on pets that already used a Command
/// this turn may flip. For <see cref="IMonsterFlipEffect"/> with <see cref="BaseMonsterCard.AskSelectFlip"/> (default true),
/// monsters stay face-down until the player picks them in the combined "Activate Flip Effects" prompt; only chosen cards
/// flip and resolve their flip effect. Cancel leaves all face-down. Cards with <c>AskSelectFlip == false</c> (e.g. Stealth Bird)
/// flip and resolve immediately with no activation prompt. Non-flip-effect face-down monsters still flip immediately (no effect).
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
public static class FlipFaceDownMonstersOnPlayerEnemyAttackPatch
{
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
        bool isLocalOwner = MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player);
        Godot.GD.Print(
            $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] evaluate owner={player.NetId} localOwner={isLocalOwner} targetCombat={target.CombatId} dealer={dealer.CombatId}");

        var promptCandidates = new List<AbstractMonsterCard>();
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!FlipFaceDownOnPlayerEnemyAttackHelpers.TryGetEligibleFaceDownSourceCard(
                    pet,
                    requireAlive: true,
                    requireUsedCommandThisTurn: false,
                    out AbstractMonsterCard? card))
                continue;

            if (card is not IMonsterFlipEffect)
            {
                if (card != null)
                    FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpNow(card, choiceContext);
                continue;
            }

            bool askPrompt = card is not BaseMonsterCard bm || bm.AskSelectFlip;
            Godot.GD.Print(
                $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] candidate owner={player.NetId} localOwner={isLocalOwner} petCombat={pet.CombatId} card={card.Id?.Entry} isFlipEffect={card is IMonsterFlipEffect} askPrompt={askPrompt}");
            if (!askPrompt)
            {
                FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpNow(card, choiceContext);
                continue;
            }

            if (!isLocalOwner)
            {
                Godot.GD.Print(
                    $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] skip prompt queue for non-local owner={player.NetId} petCombat={pet.CombatId} card={card.Id?.Entry}");
                continue;
            }

            // AskSelectFlip: keep FaceDown until the player selects this card in the activation grid (or cancels all).
            promptCandidates.Add(card);
        }

        if (promptCandidates.Count == 0)
            return;

        // Die For You pets are flipped synchronously in BeforeDamageReceived (lethal spill skips AfterDamageReceived).
        promptCandidates.RemoveAll(c => IsFaceDownCardOnDieForYouPet(player, c));

        if (promptCandidates.Count == 0)
            return;

        EnqueueFlipPromptCandidates(choiceContext, player, promptCandidates);
    }

    private static bool IsFaceDownCardOnDieForYouPet(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        AbstractMonsterCard card)
    {
        if (player.PlayerCombatState == null || card is not BaseMonsterCard bm)
            return false;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet.HasPower<DieForYouPower>() && DuelMonsterFieldRegistry.HasSourceCard(pet, bm))
                return true;
        }

        return false;
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

        // Multi-hit attacks can fire another AfterDamageReceived while the local player is still in the combined
        // "Activate Flip Effects" grid. Pending was already cleared at drain start, so the duplicate-card guard does
        // not run and the same Spear Cretin gets re-queued; when the first prompt closes, Drain runs again → two prompts.
        if (state.PromptActive)
        {
            Godot.GD.Print(
                $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] skip enqueue (flip activate prompt already open) owner={player.NetId}");
            return;
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
        // Same-frame hits: one process frame lets every AfterDamageReceived postfix enqueue first.
        await WaitOneProcessFrameAsync();
        // Multi-hit across adjacent frames: wait until Pending count is unchanged for two consecutive frames
        // (capped) so one combined prompt covers the whole attack sequence instead of one prompt per hit.
        await WaitFlipPromptPendingStableAsync(player.NetId);

        if (!PendingByPlayerNetId.TryGetValue(player.NetId, out PendingFlipPromptState? state))
            return;

        List<AbstractMonsterCard> promptCandidates = YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(state.Pending)
            .OfType<AbstractMonsterCard>()
            .Where(c => c.FaceDown && c.IsMutable)
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
            await FlipFaceDownOnPlayerEnemyAttackHelpers.SelectFlipEffectsToActivateAsync(
                choiceContext,
                player,
                promptCandidates);
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

    /// <summary>
    /// After the first frame, keep yielding until <see cref="PendingFlipPromptState.Pending"/>.Count is the same
    /// for two consecutive process frames (or we hit a frame cap). Multi-hit enemy attacks often call
    /// <see cref="Hook.AfterDamageReceived"/> once per hit on separate frames; without this, each hit opens its own prompt.
    /// </summary>
    private static async Task WaitFlipPromptPendingStableAsync(ulong playerNetId)
    {
        if (Godot.Engine.GetMainLoop() is not Godot.SceneTree tree)
        {
            await Task.Yield();
            return;
        }

        const int maxExtraFrames = 12;
        int stableFrames = 0;
        int? lastCount = null;
        for (int i = 0; i < maxExtraFrames; i++)
        {
            if (!PendingByPlayerNetId.TryGetValue(playerNetId, out PendingFlipPromptState? state))
                return;

            int c = state.Pending.Count;
            if (lastCount == c)
                stableFrames++;
            else
            {
                lastCount = c;
                stableFrames = 1;
            }

            if (stableFrames >= 2)
            {
                Godot.GD.Print(
                    $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] flip prompt batch stable ownerNet={playerNetId} pending={c} extraFrames={i + 1}");
                return;
            }

            await tree.ToSignal(tree, Godot.SceneTree.SignalName.ProcessFrame);
        }

        Godot.GD.Print(
            $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] flip prompt batch stable timeout ownerNet={playerNetId} lastCount={lastCount}");
    }

}

internal static class FlipFaceDownOnPlayerEnemyAttackHelpers
{
    private static readonly LocString ActivateFlipEffectsPrompt =
        new("cards", "YGODUELIST-FLIP_EFFECT.activate.selection");

    /// <summary>
    /// One Die For You pet: flip (and optional FLIP effect) before spill damage. Called from
    /// <see cref="DieForYouPowerFlipBeforeBattleDamagePatch"/> on each pet's <see cref="DieForYouPower"/>.
    /// </summary>
    public static async Task ResolveDieForYouPetFaceDownFlipBeforeBattleDamageAsync(
        PlayerChoiceContext choiceContext,
        Creature pet)
    {
        MegaCrit.Sts2.Core.Entities.Players.Player? player = pet.PetOwner;
        if (player?.PlayerCombatState == null || !pet.HasPower<DieForYouPower>())
            return;

        if (!TryGetEligibleFaceDownSourceCard(
                pet,
                requireAlive: true,
                requireUsedCommandThisTurn: false,
                out AbstractMonsterCard? card))
            return;

        if (card is not IMonsterFlipEffect)
        {
            if (card != null)
                ForceFlipFaceUpWithoutActivatingEffectNow(card, choiceContext);
            return;
        }

        bool askPrompt = card is not BaseMonsterCard bm || bm.AskSelectFlip;
        if (!askPrompt)
        {
            await ResolveFlippedFaceUpAndRunFlipEffectAsync(choiceContext, player, card);
            return;
        }

        if (!MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player))
        {
            Godot.GD.Print(
                $"[YgoDuelist][MP][FlipDieForYou] skip prompt for non-local owner={player.NetId} petCombat={pet.CombatId} card={card.Id?.Entry}");
            return;
        }

        Godot.GD.Print(
            $"[YgoDuelist][MP][FlipDieForYou] flip prompt before battle damage owner={player.NetId} petCombat={pet.CombatId} card={card.Id?.Entry}");

        await SelectFlipEffectsToActivateAsync(choiceContext, player, new[] { card });
    }

    private static async Task ResolveFlippedFaceUpAndRunFlipEffectAsync(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        AbstractMonsterCard card)
    {
        if (!ForceFlipFaceUpWithoutActivatingEffectNow(card, choiceContext))
            return;
        if (card is not IMonsterFlipEffect flip)
            return;

        await YgoMonsterFlipEffectRunner.RunFlipEffectAsync(
            flip,
            YgoChoiceContexts.Blocking(choiceContext),
            card);
    }

    public static async Task SelectFlipEffectsToActivateAsync(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        IReadOnlyList<AbstractMonsterCard> promptCandidates)
    {
        int maxPick = promptCandidates.Count;
        var prefs = new CardSelectorPrefs(ActivateFlipEffectsPrompt, 1, maxPick)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        Godot.GD.Print(
            $"[YgoDuelist][MP][FlipFaceDownOnEnemyAttack] showing combined activate prompt owner={player.NetId} candidates={maxPick} min=1 cancelable=true (face-down until chosen; cancel skips all)");

        List<AbstractMonsterCard> selected = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            prefs,
            () => YgoMpCombatOrder.CardsSnapshotOrderedForMp(promptCandidates)
                .OfType<AbstractMonsterCard>()
                .Where(c => c.FaceDown && c.IsMutable)
                .ToList(),
            promptCandidates.Count);
        foreach (AbstractMonsterCard selectedCard in selected)
        {
            if (selectedCard is not IMonsterFlipEffect flip)
                continue;
            if (!selectedCard.FaceDown || !selectedCard.IsMutable)
                continue;

            if (!ForceFlipFaceUpWithoutActivatingEffectNow(selectedCard, choiceContext))
            {
                Godot.GD.PrintErr(
                    $"[YgoDuelist][FlipFaceDownOnEnemyAttack] flip+effect skipped: MarkFlippedFaceUpOnField failed card={selectedCard.Id?.Entry} ownerNet={player.NetId}");
                continue;
            }

            await YgoMonsterFlipEffectRunner.RunFlipEffectAsync(
                flip,
                YgoChoiceContexts.Blocking(choiceContext),
                selectedCard);
        }
    }

    public static bool TryGetEligibleFaceDownSourceCard(Creature pet, out AbstractMonsterCard? card)
    {
        return TryGetEligibleFaceDownSourceCard(
            pet,
            requireAlive: true,
            requireUsedCommandThisTurn: true,
            out card);
    }

    public static bool TryGetEligibleFaceDownSourceCard(Creature pet, bool requireAlive, out AbstractMonsterCard? card)
    {
        return TryGetEligibleFaceDownSourceCard(
            pet,
            requireAlive,
            requireUsedCommandThisTurn: true,
            out card);
    }

    public static bool TryGetEligibleFaceDownSourceCard(
        Creature pet,
        bool requireAlive,
        bool requireUsedCommandThisTurn,
        out AbstractMonsterCard? card)
    {
        card = null;
        if (requireAlive && !pet.IsAlive)
            return false;
        if (requireUsedCommandThisTurn && !MonsterCommandRegistry.PetHasUsedAnyCommandSlotThisTurn(pet))
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
