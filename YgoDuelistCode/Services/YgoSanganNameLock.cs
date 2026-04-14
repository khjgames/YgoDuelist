using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Sangan: after adding a searched monster to hand, that card's <see cref="CardModel.Id.Entry"/> cannot be played for the rest of the turn.
/// </summary>
public static class YgoSanganNameLock
{
    private static readonly Dictionary<Player, string> LockedEntryByPlayer = new();

    public static void Set(Player player, string cardIdEntry)
    {
        if (player == null || string.IsNullOrEmpty(cardIdEntry))
            return;
        LockedEntryByPlayer[player] = cardIdEntry;
    }

    public static void Clear(Player player)
    {
        if (player == null)
            return;
        LockedEntryByPlayer.Remove(player);
    }

    public static bool IsLocked(Player? player, CardModel? card)
    {
        if (player == null || card == null)
            return false;
        return LockedEntryByPlayer.TryGetValue(player, out string? e) && e == card.Id.Entry;
    }

    public static void ClearAll() => LockedEntryByPlayer.Clear();
}
