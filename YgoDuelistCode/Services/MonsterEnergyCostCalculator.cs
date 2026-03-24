using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Z = combat-scale ATK or DEF (YGO/100). Maps level + ritual/fusion + Z to summon/command energy 0–3.
/// </summary>
public static class MonsterEnergyCostCalculator
{
    /// <summary>Level, card type, and Z stat (ATK or DEF scale). Unknown stats force cost 1.</summary>
    public static int Compute(int level, YgoCardType ygoType, int z, bool statsUnknown)
    {
        if (statsUnknown || z < 0)
            return 1;

        bool ritualOrFusion = ygoType == YgoCardType.RitualMonster || ygoType == YgoCardType.FusionMonster;

        if (level <= 4)
            return ritualOrFusion ? CostRitualFusionLow(z) : CostNormalLow(z);
        if (level <= 6)
            return CostMid56(z);
        if (level <= 8)
            return CostMid78(z);
        if (level <= 10)
            return CostHigh910(z);
        return CostLevel11Plus(z);
    }

    private static int CostNormalLow(int z)
    {
        if (z <= 3) return 0;
        if (z <= 11) return 1;
        if (z <= 20) return 2;
        return 3;
    }

    private static int CostRitualFusionLow(int z)
    {
        if (z <= 6) return 0;
        if (z <= 14) return 1;
        if (z <= 23) return 2;
        return 3;
    }

    private static int CostMid56(int z)
    {
        if (z <= 8) return 0;
        if (z <= 16) return 1;
        if (z <= 25) return 2;
        return 3;
    }

    private static int CostMid78(int z)
    {
        if (z <= 10) return 0;
        if (z <= 17) return 1;
        if (z <= 30) return 2;
        return 3;
    }

    private static int CostHigh910(int z)
    {
        if (z <= 11) return 0;
        if (z <= 25) return 1;
        if (z <= 45) return 2;
        return 3;
    }

    private static int CostLevel11Plus(int z)
    {
        if (z <= 12) return 0;
        if (z <= 29) return 1;
        if (z <= 60) return 2;
        return 3;
    }
}
