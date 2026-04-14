using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per-player monotonic counter bumped at combat turn start; used for same-turn GY / banish triggers.
/// </summary>
public static class YgoPlayerCombatTurnStamp
{
    private static readonly Dictionary<Player, int> StampByPlayer = new();

    public static void Bump(Player player)
    {
        if (player == null)
            return;
        StampByPlayer.TryGetValue(player, out int v);
        StampByPlayer[player] = v + 1;
    }

    public static int Get(Player player) =>
        player != null && StampByPlayer.TryGetValue(player, out int v) ? v : 0;

    public static void ClearAll() => StampByPlayer.Clear();
}
