using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoBadReactionToSimochi
{
    /// <summary>
    /// Picks the active face-up Simochi in any player's Spell/Trap zone with the highest damage multiplier.
    /// </summary>
    public static bool TryResolveBest(CombatState cs, out CardModel? sourceCard, out decimal damageMultiplier)
    {
        sourceCard = null;
        damageMultiplier = 1m;
        CardModel? best = null;
        decimal bestMult = 0m;

        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
            if (zone == null)
                continue;

            foreach (CardModel c in zone.Cards)
            {
                if (c is not BaseContinuousTrapCard trap || trap.FaceDown)
                    continue;
                if (c is not IYgoBadReactionToSimochiHealRedirect redir)
                    continue;

                decimal m = redir.GetEnemyHealRedirectDamageMultiplier();
                if (m > bestMult)
                {
                    bestMult = m;
                    best = c;
                }
            }
        }

        if (best == null || bestMult <= 0m)
            return false;

        sourceCard = best;
        damageMultiplier = bestMult;
        return true;
    }

    public static bool IsCombatEnemy(CombatState cs, Creature creature)
        => cs.Enemies.Any(e => ReferenceEquals(e, creature));
}
