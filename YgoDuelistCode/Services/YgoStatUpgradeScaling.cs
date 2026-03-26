namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Monster printed ATK/DEF/MGC (Cards_Revised chunk W): +2/+3/+4/+5 by breakpoints 15/22/29.
/// Spell/trap/equip/field printed boost amounts use <see cref="GetSpellTrapStatBonusUpgradeDelta"/> instead.
/// </summary>
public static class YgoStatUpgradeScaling
{
    /// <summary>Upgrade delta for each printed monster ATK/DEF/MGC line (independent per stat).</summary>
    public static int GetMonsterPrintedStatUpgradeBonus(int baseStat)
    {
        if (baseStat >= 29)
            return 5;
        if (baseStat >= 22)
            return 4;
        if (baseStat >= 15)
            return 3;
        return 2;
    }

    /// <summary>
    /// Upgrade delta for spell/trap/equip/field printed ATK or DEF boost magnitudes (not monster body stats).
    /// ≤4: +2; 5–7: +3; ≥8: 1 + floor(35% of printed).
    /// </summary>
    public static int GetSpellTrapStatBonusUpgradeDelta(int printedMagnitude)
    {
        if (printedMagnitude <= 0)
            return 0;
        if (printedMagnitude <= 4)
            return 2;
        if (printedMagnitude <= 7)
            return 3;
        return 1 + (int)System.Math.Floor(printedMagnitude * 0.35m);
    }

    /// <summary>
    /// Applies spell/trap stat-boost upgrade to a signed ATK/DEF component (penalties move further negative by the same delta magnitude).
    /// </summary>
    public static int ApplySpellTrapStatBonusUpgrade(int printedSigned, bool isUpgraded)
    {
        if (!isUpgraded || printedSigned == 0)
            return printedSigned;
        int mag = System.Math.Abs(printedSigned);
        int d = GetSpellTrapStatBonusUpgradeDelta(mag);
        return printedSigned > 0 ? printedSigned + d : printedSigned - d;
    }
}
