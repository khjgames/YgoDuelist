using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoIntentAttackDamage
{
    /// <summary>Sum of <see cref="AttackIntent"/> damage this enemy would deal to <paramref name="playerTarget"/> on its current move.</summary>
    public static int GetTotalAttackIntentDamage(Creature enemy, Creature playerTarget)
    {
        if (enemy.Monster?.NextMove?.Intents == null)
            return 0;

        var targets = new List<Creature> { playerTarget };
        int sum = 0;
        foreach (AbstractIntent intent in enemy.Monster.NextMove.Intents)
        {
            if (intent is AttackIntent atk)
                sum += atk.GetTotalDamage(targets, enemy);
        }

        return sum;
    }
}
