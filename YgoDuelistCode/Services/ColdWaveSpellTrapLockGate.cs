using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class ColdWaveSpellTrapLockGate
{
    private static readonly HashSet<Player> SpellTrapActivatedOrSetThisTurn = new();

    /// <summary>
    /// True after this player has played (activated) or Set any Spell/Trap this turn.
    /// Cold Wave may only be activated while this is false.
    /// </summary>
    public static bool HasPlayerUsedSpellTrapThisTurn(Player? player) =>
        player != null && SpellTrapActivatedOrSetThisTurn.Contains(player);

    public static void MarkPlayerUsedSpellTrapThisTurn(Player? player)
    {
        if (player != null)
            SpellTrapActivatedOrSetThisTurn.Add(player);
    }

    public static void ResetSpellTrapUsageForPlayerTurnStart(Player? player)
    {
        if (player != null)
            SpellTrapActivatedOrSetThisTurn.Remove(player);
    }

    public static bool IsPlayerLockedThisTurn(Player? player)
    {
        Creature? c = player?.Creature;
        return c != null && c.GetPower<ColdWaveSpellTrapLockPower>() != null;
    }
}
