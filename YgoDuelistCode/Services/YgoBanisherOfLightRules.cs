using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While a face-up <see cref="Banisher_of_the_Light"/> is on either player's field, cards that would be sent to the Graveyard are banished instead (Yu-Gi-Oh! continuous effect).
/// </summary>
public static class YgoBanisherOfLightRules
{
    public static bool IsBanisherRedirectActive(CombatState? cs)
    {
        if (cs == null)
            return false;

        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
            {
                if (m is Banisher_of_the_Light b && !b.FaceDown)
                    return true;
            }
        }

        return false;
    }
}
