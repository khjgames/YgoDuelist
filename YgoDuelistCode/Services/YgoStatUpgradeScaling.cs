namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Combat-scale stat bonuses on upgrade: +2 default, +3 if base ≥15, +4 if ≥22, +5 if ≥29.
/// </summary>
public static class YgoStatUpgradeScaling
{
    public static int GetStatUpgradeBonus(int baseStat)
    {
        if (baseStat >= 29)
            return 5;
        if (baseStat >= 22)
            return 4;
        if (baseStat >= 15)
            return 3;
        return 2;
    }
}
