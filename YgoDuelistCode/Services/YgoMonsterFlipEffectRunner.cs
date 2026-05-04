using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoMonsterFlipEffectRunner
{
    private static readonly LocString ResolvingFlipEffectPrompt =
        new("cards", "YGODUELIST-FLIP_EFFECT.resolving.selection");

    /// <summary>
    /// Call after <paramref name="card"/> was face-down and is now face-up on the field.
    /// </summary>
    public static void ScheduleIfFlippedOnField(AbstractMonsterCard card, bool wasFaceDownBefore, PlayerChoiceContext? choiceContext)
    {
        if (!MarkFlippedFaceUpOnField(card, wasFaceDownBefore))
            return;
        if (card is not IMonsterFlipEffect flip)
            return;

        PlayerChoiceContext ctx = YgoChoiceContexts.Blocking(choiceContext);
        TaskHelper.RunSafely(RunFlipEffectAsync(flip, ctx, card));
    }

    /// <summary>
    /// Records shared "this monster flipped face-up" state after the caller has already changed
    /// <see cref="AbstractMonsterCard.FaceDown"/> to false. This is separate from activating the optional flip effect
    /// so prompt-based flips can become face-up before the synced choice resolves.
    /// </summary>
    public static bool MarkFlippedFaceUpOnField(AbstractMonsterCard card, bool wasFaceDownBefore)
    {
        if (!wasFaceDownBefore || card.FaceDown)
            return false;
        if (!card.IsMutable)
            return false;
        if (card is not BaseMonsterCard bm || bm.Owner == null)
            return false;
        if (!IsRegisteredOrPetLinkedFieldMonster(bm))
            return false;

        bm.ScheduleFlipFaceUpSideEffectsBeforeFlipPipeline();
        bm.FlippedThisTurn = true;
        return true;
    }

    public static async Task RunFlipEffectAsync(IMonsterFlipEffect flip, PlayerChoiceContext ctx, AbstractMonsterCard self)
    {
        if (self is BaseMonsterCard monster && monster.Owner != null)
            await YgoPreviewGridSelection.ShowPreviewAsync(ctx, new[] { self }, monster.Owner, ResolvingFlipEffectPrompt);

        await flip.OnFlippedFaceUpAsync(ctx, self);
    }

    /// <summary>
    /// <see cref="DuelMonsterFieldRegistry.ContainsFieldMonster"/> is the fast path; during damage hooks / MP
    /// reordering the HashSet can briefly disagree with <see cref="DuelMonsterFieldRegistry.HasSourceCard"/> pet links.
    /// </summary>
    private static bool IsRegisteredOrPetLinkedFieldMonster(BaseMonsterCard bm)
    {
        if (DuelMonsterFieldRegistry.ContainsFieldMonster(bm.Owner, bm))
            return true;
        if (bm.Owner.PlayerCombatState == null)
            return false;
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(bm.Owner.PlayerCombatState))
        {
            if (DuelMonsterFieldRegistry.HasSourceCard(pet, bm))
                return true;
        }

        return false;
    }
}
