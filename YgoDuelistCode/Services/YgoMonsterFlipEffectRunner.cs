using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoMonsterFlipEffectRunner
{
    /// <summary>
    /// Call after <paramref name="card"/> was face-down and is now face-up on the field.
    /// </summary>
    public static void ScheduleIfFlippedOnField(AbstractMonsterCard card, bool wasFaceDownBefore, PlayerChoiceContext? choiceContext)
    {
        if (!wasFaceDownBefore || card.FaceDown)
            return;
        if (card is not BaseMonsterCard bm || bm.Owner == null)
            return;
        if (!DuelMonsterFieldRegistry.GetFieldMonsters(bm.Owner).Contains(bm))
            return;
        if (card is not IMonsterFlipEffect flip)
            return;

        PlayerChoiceContext ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        TaskHelper.RunSafely(RunFlipAsync(flip, ctx, card));
    }

    private static async Task RunFlipAsync(IMonsterFlipEffect flip, PlayerChoiceContext ctx, AbstractMonsterCard self) =>
        await flip.OnFlippedFaceUpAsync(ctx, self);
}
