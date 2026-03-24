using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Once-per-player-turn flags (plan: "annual"). Reset on <see cref="GraveyardRelic.AfterPlayerTurnStart"/>.
/// </summary>
public static class YgoAnnualTracker
{
    public static bool TryConsumeAnnual(Player? player, string key)
    {
        var relic = player?.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        return relic != null && relic.TryConsumeAnnual(key);
    }
}
