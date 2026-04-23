using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
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
        if (!wasFaceDownBefore || card.FaceDown)
            return;
        if (!card.IsMutable)
            return;
        if (card is not BaseMonsterCard bm || bm.Owner == null)
            return;
        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(bm.Owner, bm))
            return;
        bm.ScheduleFlipFaceUpSideEffectsBeforeFlipPipeline();
        bm.FlippedThisTurn = true;
        if (card is not IMonsterFlipEffect flip)
            return;

        PlayerChoiceContext ctx = YgoChoiceContexts.Blocking(choiceContext);
        TaskHelper.RunSafely(RunFlipAsync(flip, ctx, card));
    }

    private static async Task RunFlipAsync(IMonsterFlipEffect flip, PlayerChoiceContext ctx, AbstractMonsterCard self)
    {
        if (self is BaseMonsterCard monster && monster.Owner != null)
            await YgoPreviewGridSelection.ShowPreviewAsync(ctx, new[] { self }, monster.Owner, ResolvingFlipEffectPrompt);

        await flip.OnFlippedFaceUpAsync(ctx, self);
    }
}
