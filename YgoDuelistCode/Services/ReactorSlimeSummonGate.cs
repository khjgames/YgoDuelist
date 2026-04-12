using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Cards.Monster.Todo.Effect.Reactor_Slime"/> effect 1: for the rest of the turn, only Divine-Beast monsters may be normal or special summoned.
/// </summary>
public static class ReactorSlimeSummonGate
{
    private static readonly HashSet<Player> _restricted = new();

    public static void MarkRestricted(Player? player)
    {
        if (player != null)
            _restricted.Add(player);
    }

    public static bool BlocksNonDivineSummons(Player? player) =>
        player != null && _restricted.Contains(player);

    public static bool AllowsSummon(Player? player, BaseMonsterCard? card)
    {
        if (!BlocksNonDivineSummons(player))
            return true;
        return card != null && card.DuelMonsterRace == DuelMonsterRace.DivineBeast;
    }

    public static void ResetForPlayer(Player? player)
    {
        if (player != null)
            _restricted.Remove(player);
    }

    public static void ClearAll() => _restricted.Clear();
}
