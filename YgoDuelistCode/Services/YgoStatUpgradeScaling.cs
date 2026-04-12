using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Monster printed ATK/DEF/MGC smith: table-driven per <c>Monster_Stat_Scaling_Balanced.md</c>.
/// Spell/trap/equip/field printed boost amounts use <see cref="GetSpellTrapStatBonusUpgradeDelta"/> instead.
/// </summary>
public static class YgoStatUpgradeScaling
{
    /// <summary>
    /// Legacy smith deltas for effect monsters level ≤4 only (unchanged breakpoints 15/22/29).
    /// </summary>
    public static int GetLegacyMonsterSmithDelta(int baseStat)
    {
        if (baseStat >= 29)
            return 5;
        if (baseStat >= 22)
            return 4;
        if (baseStat >= 15)
            return 3;
        return 2;
    }

    /// <summary>Obsolete name: use <see cref="GetLegacyMonsterSmithDelta"/> or <see cref="GetMonsterPrintedLineUpgradeDelta"/>.</summary>
    public static int GetMonsterPrintedStatUpgradeBonus(int baseStat) => GetLegacyMonsterSmithDelta(baseStat);

    /// <summary>
    /// First smith upgrade on a printed ATK, DEF, or MGC line. <paramref name="baseStatLine"/> is the unupgraded value;
    /// for DEF, pass printed DEF (virtual ATK = DEF+1 for table matching is applied internally).
    /// </summary>
    public static int GetMonsterPrintedLineUpgradeDelta(
        int level,
        YgoCardType ygoType,
        int unupgradedPlayEnergyForLine,
        int baseStatLine,
        bool isDefenseLine)
    {
        if (ygoType == YgoCardType.EffectMonster && level <= 4)
            return GetLegacyMonsterSmithDelta(baseStatLine);

        int costCol = unupgradedPlayEnergyForLine <= 1 ? 1 : 2;
        int matchStat = isDefenseLine ? baseStatLine + 1 : baseStatLine;

        bool fusion = ygoType == YgoCardType.FusionMonster;
        bool ritual = ygoType == YgoCardType.RitualMonster;
        bool normalOrEffect = ygoType == YgoCardType.Monster || ygoType == YgoCardType.EffectMonster;

        if (level <= 4)
        {
            if (normalOrEffect)
                return SmithNormalLow4(costCol, matchStat);
            if (ritual || fusion)
                return SmithRitualFusionLow4(costCol, matchStat);
        }

        if (level <= 6)
        {
            if (normalOrEffect)
                return SmithNormalEffect56(costCol, matchStat);
            if (ritual || fusion)
                return SmithRitualFusion56(costCol, matchStat);
        }

        if (level <= 8)
        {
            if (fusion)
                return SmithFusion78(costCol, matchStat);
            if (normalOrEffect || ritual)
                return SmithNormalRitualEffect78(costCol, matchStat);
        }

        if (level <= 10)
            return SmithAll910(costCol, matchStat);

        return SmithAll11Plus(costCol, matchStat);
    }

    /// <summary>MGC uses the same smith mapping as ATK for the same level and card type.</summary>
    public static int GetMonsterMgcUpgradeDelta(int level, YgoCardType ygoType, int baseMgc, bool statsUnknown)
    {
        int e = MonsterEnergyCostCalculator.GetMonsterPlayEnergy(level, ygoType, baseMgc, true, false, statsUnknown);
        return GetMonsterPrintedLineUpgradeDelta(level, ygoType, e, baseMgc, isDefenseLine: false);
    }

    private static int SmithNormalLow4(int costCol, int s)
    {
        if (costCol == 1 && s == 7) return 2;
        if (costCol == 1 && s == 8) return 2;
        if (costCol == 2 && s == 9) return 5;
        if (costCol == 2 && s >= 10 && s <= 12) return 13 - s;
        if (costCol == 2 && s >= 13 && s <= 14) return 16 - s;
        if (costCol == 2 && s >= 15 && s <= 16) return 19 - s;
        if (costCol == 2 && s == 17) return 3;
        if (costCol == 2 && s >= 18 && s <= 21) return 3;
        if (costCol == 2 && s >= 22) return 4;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithRitualFusionLow4(int costCol, int s)
    {
        if (costCol == 1 && s == 11) return 3;
        if (costCol == 1 && s == 12) return 3;
        if (costCol == 2 && s == 13) return 6;
        if (costCol == 2 && s >= 14 && s <= 16) return 18 - s;
        if (costCol == 2 && s >= 17 && s <= 18) return 21 - s;
        if (costCol == 2 && s >= 19 && s <= 20) return 24 - s;
        if (costCol == 2 && s == 21) return 4;
        if (costCol == 2 && s >= 22 && s <= 25) return 4;
        if (costCol == 2 && s >= 26) return 5;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithNormalEffect56(int costCol, int s)
    {
        if (costCol == 1 && s == 12) return 3;
        if (costCol == 1 && s == 13) return 3;
        if (costCol == 2 && s == 14) return 6;
        if (costCol == 2 && s >= 15 && s <= 17) return 19 - s;
        if (costCol == 2 && s >= 18 && s <= 19) return 22 - s;
        if (costCol == 2 && s >= 20 && s <= 21) return 25 - s;
        if (costCol == 2 && s == 22) return 4;
        if (costCol == 2 && s >= 23 && s <= 26) return 4;
        if (costCol == 2 && s >= 27) return 5;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithRitualFusion56(int costCol, int s)
    {
        if (costCol == 1 && s == 13) return 4;
        if (costCol == 1 && s == 14) return 4;
        if (costCol == 2 && s == 15) return 7;
        if (costCol == 2 && s >= 16 && s <= 18) return 21 - s;
        if (costCol == 2 && s >= 19 && s <= 20) return 24 - s;
        if (costCol == 2 && s >= 21 && s <= 22) return 27 - s;
        if (costCol == 2 && s == 23) return 5;
        if (costCol == 2 && s >= 24 && s <= 27) return 5;
        if (costCol == 2 && s >= 28) return 6;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithNormalRitualEffect78(int costCol, int s)
    {
        if (costCol == 1 && s == 16) return 4;
        if (costCol == 1 && s == 17) return 4;
        if (costCol == 2 && s == 18) return 7;
        if (costCol == 2 && s >= 19 && s <= 21) return 24 - s;
        if (costCol == 2 && s >= 22 && s <= 23) return 27 - s;
        if (costCol == 2 && s >= 24 && s <= 25) return 30 - s;
        if (costCol == 2 && s == 26) return 5;
        if (costCol == 2 && s >= 27 && s <= 30) return 5;
        if (costCol == 2 && s >= 31) return 6;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithFusion78(int costCol, int s)
    {
        if (costCol == 1 && s == 18) return 5;
        if (costCol == 1 && s == 19) return 5;
        if (costCol == 2 && s == 20) return 8;
        if (costCol == 2 && s >= 21 && s <= 23) return 27 - s;
        if (costCol == 2 && s >= 24 && s <= 25) return 30 - s;
        if (costCol == 2 && s >= 26 && s <= 27) return 33 - s;
        if (costCol == 2 && s == 28) return 6;
        if (costCol == 2 && s >= 29 && s <= 32) return 6;
        if (costCol == 2 && s >= 33) return 7;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithAll910(int costCol, int s)
    {
        if (costCol == 1 && s == 25) return 5;
        if (costCol == 1 && s == 26) return 5;
        if (costCol == 2 && s == 27) return 8;
        if (costCol == 2 && s >= 28 && s <= 30) return 34 - s;
        if (costCol == 2 && s >= 31 && s <= 32) return 37 - s;
        if (costCol == 2 && s >= 33 && s <= 34) return 40 - s;
        if (costCol == 2 && s == 35) return 6;
        if (costCol == 2 && s >= 36 && s <= 39) return 6;
        if (costCol == 2 && s >= 40) return 7;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithAll11Plus(int costCol, int s)
    {
        if (costCol == 1 && s == 29) return 5;
        if (costCol == 1 && s == 30) return 5;
        if (costCol == 2 && s == 31) return 8;
        if (costCol == 2 && s >= 32 && s <= 34) return 38 - s;
        if (costCol == 2 && s >= 35 && s <= 36) return 41 - s;
        if (costCol == 2 && s >= 37 && s <= 38) return 44 - s;
        if (costCol == 2 && s == 39) return 6;
        if (costCol == 2 && s >= 40 && s <= 43) return 6;
        if (costCol == 2 && s >= 44) return 7;
        return GetLegacyMonsterSmithDelta(s);
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
