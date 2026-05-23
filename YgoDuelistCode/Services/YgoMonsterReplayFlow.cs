using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

internal static class YgoMonsterReplayFlow
{
    public static async Task ExecuteReplayAsync(
        PlayerChoiceContext choiceContext,
        YgoReplayContext context)
    {
        Player player = context.Player;
        if (player.Creature?.IsDead == true || CombatManager.Instance.IsOverOrEnding)
            return;

        BaseMonsterCard? template = ResolveTemplateMonster(context);
        if (template == null)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, tributeReleaseCount: 0))
        {
            CardModel fizzleClone = YgoReplayCloneFactory.CreatePersistentClone(template);
            await YgoReplayFizzle.SendToGraveyardAsync(player, fizzleClone);
            return;
        }

        BaseMonsterCard clone = (BaseMonsterCard)YgoReplayCloneFactory.CreatePersistentClone(template);
        clone.CopyDisplayFormFrom(template);

        bool canAttackThisTurn = context.Route == YgoReplayRoute.MonsterHandPlay
            ? context.HandSummonCanAttackThisTurn
            : true;

        bool summoned = await DuelMonsterSummon.TrySummonReplayDuplicateAsync(
            player,
            clone,
            choiceContext,
            context.Target,
            canAttackThisTurn);

        if (!summoned)
        {
            await YgoReplayFizzle.SendToGraveyardAsync(player, clone);
            return;
        }

        if (clone is NormalMonsterCard normal)
            await TryRunHandSummonCombatActionAsync(choiceContext, normal, context);
    }

    private static async Task TryRunHandSummonCombatActionAsync(
        PlayerChoiceContext choiceContext,
        NormalMonsterCard clone,
        YgoReplayContext context)
    {
        if (context.HandSummonCombat == YgoReplayHandSummonCombatKind.None)
            return;

        Creature? target = context.HandSummonCombat == YgoReplayHandSummonCombatKind.Attack
            ? context.Target
            : null;

        if (context.HandSummonCombat == YgoReplayHandSummonCombatKind.Attack
            && (target == null || !target.IsAlive))
        {
            return;
        }

        var cardPlay = new CardPlay
        {
            Card = clone,
            Target = target,
            ResultPile = PileType.None,
            Resources = new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = 0,
                StarsSpent = 0,
                StarValue = 0,
            },
            IsAutoPlay = false,
            PlayIndex = 0,
            PlayCount = 1,
        };

        await clone.CombatAction(choiceContext, cardPlay);
    }

    private static BaseMonsterCard? ResolveTemplateMonster(YgoReplayContext context) =>
        context.Route switch
        {
            YgoReplayRoute.MonsterOutputOnly => context.SummonedOutputMonster,
            YgoReplayRoute.MonsterHandPlay => context.SourceCard as BaseMonsterCard,
            _ => null,
        };
}
