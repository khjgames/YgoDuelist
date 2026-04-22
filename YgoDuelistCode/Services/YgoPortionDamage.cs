using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoPortionDamage
{
    /// <summary>
    /// Deals this monster's attack to <paramref name="target"/> as one hit or as a Portion salvo (2–5 hits).
    /// Returns the last <see cref="AttackCommand"/> when any damage was attempted, otherwise null.
    /// </summary>
    public static async Task<AttackCommand?> DealMonsterAttackToTargetAsync(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard monster,
        Creature target,
        int attackTotal,
        string hitFx)
    {
        int n = monster.AttackPortionCount;
        if (n < 2)
            return await SingleMonsterAttackAsync(choiceContext, monster, target, attackTotal, hitFx);

        int[] portions = YgoPortionMath.SplitTotalIntoPortions(attackTotal, n);
        YgoPortionedSalvo.BeginMonsterSalvo(monster, target, portions);
        AttackCommand? last = null;
        foreach (int chunk in portions)
        {
            last = await DamageCmd.Attack((decimal)chunk)
                .FromCard(monster)
                .Targeting(target)
                .WithHitFx(hitFx)
                .Execute(choiceContext);
        }

        return last;
    }

    /// <summary>
    /// Deals attack damage from a <see cref="YgoDuelistCard"/> (spell/trap/strike) in one or Portioned hits.
    /// </summary>
    public static async Task<AttackCommand?> DealCardAttackDamageAsync(
        PlayerChoiceContext choiceContext,
        YgoDuelistCard card,
        Creature target,
        decimal damageTotal,
        string hitFx)
    {
        int n = card.CardDamagePortionCount;
        if (n < 2)
        {
            return await DamageCmd.Attack(damageTotal)
                .FromCard(card)
                .Targeting(target)
                .WithHitFx(hitFx)
                .Execute(choiceContext);
        }

        int total = (int)decimal.Floor(damageTotal);
        if (total < 0)
            total = 0;

        int[] portions = YgoPortionMath.SplitTotalIntoPortions(total, n);
        YgoPortionedSalvo.BeginCardSalvo(card, target, portions);
        AttackCommand? last = null;
        foreach (int chunk in portions)
        {
            last = await DamageCmd.Attack((decimal)chunk)
                .FromCard(card)
                .Targeting(target)
                .WithHitFx(hitFx)
                .Execute(choiceContext);
        }

        return last;
    }

    private static async Task<AttackCommand?> SingleMonsterAttackAsync(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard monster,
        Creature target,
        int attackTotal,
        string hitFx) =>
        await DamageCmd.Attack((decimal)attackTotal)
            .FromCard(monster)
            .Targeting(target)
            .WithHitFx(hitFx)
            .Execute(choiceContext);
}
