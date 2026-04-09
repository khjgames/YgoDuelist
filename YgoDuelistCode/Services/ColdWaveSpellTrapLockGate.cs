using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class ColdWaveSpellTrapLockGate
{
    public static bool IsPlayerLockedThisTurn(Player? player)
    {
        Creature? c = player?.Creature;
        return c != null && c.GetPower<ColdWaveSpellTrapLockPower>() != null;
    }
}
