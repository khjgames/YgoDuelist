using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoBadReactionToSimochi
{
    /// <summary>
    /// Picks the active face-up Simochi in any player's Spell/Trap zone with the highest damage multiplier.
    /// </summary>
    public static bool TryResolveBest(CombatState cs, out Bad_Reaction_to_Simochi? simochi, out decimal damageMultiplier)
    {
        simochi = null;
        damageMultiplier = 1m;
        Bad_Reaction_to_Simochi? best = null;
        decimal bestMult = 0m;

        foreach (Player player in cs.Players)
        {
            CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
            if (zone == null)
                continue;

            foreach (CardModel c in zone.Cards)
            {
                if (c is not Bad_Reaction_to_Simochi br || br.FaceDown)
                    continue;

                decimal m = br.IsUpgraded ? 1.5m : 1m;
                if (m > bestMult)
                {
                    bestMult = m;
                    best = br;
                }
            }
        }

        if (best == null || bestMult <= 0m)
            return false;

        simochi = best;
        damageMultiplier = bestMult;
        return true;
    }

    public static bool IsCombatEnemy(CombatState cs, Creature creature)
        => cs.Enemies.Any(e => ReferenceEquals(e, creature));
}
