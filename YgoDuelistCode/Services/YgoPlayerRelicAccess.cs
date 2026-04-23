using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared accessors for singleton-style YGO relic lookups on a player.
/// Callers keep the gameplay rule; this helper avoids repeating typed first-match scans.
/// </summary>
public static class YgoPlayerRelicAccess
{
    public static TRelic? GetRelic<TRelic>(Player? player)
        where TRelic : RelicModel =>
        player?.Relics.OfType<TRelic>().FirstOrDefault();

    public static IEnumerable<TRelic> GetRelics<TRelic>(Player? player)
        where TRelic : RelicModel =>
        player?.Relics.OfType<TRelic>() ?? Enumerable.Empty<TRelic>();
}
