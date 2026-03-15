using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Tracks whether each player has used their one normal summon this turn.
/// Reset on player turn start. Monster Reborn (special summon) does not use this.
/// </summary>
public static class NormalSummonTracker
{
    private static readonly HashSet<Player> _usedThisTurn = new();

    public static bool HasUsedThisTurn(Player? player)
    {
        return player != null && _usedThisTurn.Contains(player);
    }

    public static void MarkUsed(Player? player)
    {
        if (player != null)
            _usedThisTurn.Add(player);
    }

    public static void ResetForPlayer(Player? player)
    {
        if (player != null)
            _usedThisTurn.Remove(player);
    }

    /// <summary>Clears all tracking. Call at end of combat.</summary>
    public static void ClearAll()
    {
        _usedThisTurn.Clear();
    }
}
