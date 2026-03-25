namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Cards_Revised.md chunk W — upgrade delta for printed ATK/DEF/MGC (monsters) and similar combat-scale stats on spells/traps/equips:
/// +2 if base &lt; 15, +3 if ≥15, +4 if ≥22, +5 if ≥29 (evaluated per stat independently).
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
