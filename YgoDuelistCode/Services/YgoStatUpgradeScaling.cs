using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Monster printed ATK/DEF/MGC smith: table-driven per <c>Monster_Stat_Scaling_Balanced.md</c>.
/// Spell/trap/equip/field printed boost amounts use <see cref="GetSpellTrapStatBonusUpgradeDelta"/> instead.
/// </summary>
public static class YgoStatUpgradeScaling
{
    /// <summary>
    /// Smith <b>Upgraded cost</b> for this stance, keyed only by <b>unupgraded</b> constructor printed stat — not by post-upgrade Z
    /// (see <c>Monster_Stat_Scaling_Balanced.md</c> smith tables). Effect L≤4 returns false (legacy Z “annotation” path).
    /// </summary>
    public static bool TryGetSmithUpgradedPlayEnergy(
        int level,
        YgoCardType ygoType,
        bool isDefenseLine,
        int constructorBaseStatLine,
        bool statsUnknown,
        out int designatedPlayEnergy)
    {
        designatedPlayEnergy = 0;
        if (statsUnknown || constructorBaseStatLine < 0)
            return false;
        if (ygoType == YgoCardType.EffectMonster && level <= 4)
            return false;

        bool isAttackStat = !isDefenseLine;
        int unupgradedE = MonsterEnergyCostCalculator.GetUnupgradedPlayEnergyFromZBand(
            level, ygoType, constructorBaseStatLine, isAttackStat, statsUnknown);

        int costCol = unupgradedE <= 1 ? 1 : 2;
        bool fusionLow = ygoType == YgoCardType.FusionMonster && level <= 8;
        int matchStat = fusionLow
            ? constructorBaseStatLine
            : (isDefenseLine ? constructorBaseStatLine + 1 : constructorBaseStatLine);
        if (!fusionLow && ygoType == YgoCardType.Monster && level <= 4 && isDefenseLine && constructorBaseStatLine == 4)
            matchStat = 4;

        int? cost = LookupSmithUpgradedPlayEnergyCost(level, ygoType, costCol, matchStat);
        if (!cost.HasValue)
            return false;
        designatedPlayEnergy = cost.Value;
        return true;
    }

    private static int? LookupSmithUpgradedPlayEnergyCost(int level, YgoCardType ygoType, int costCol, int matchStat)
    {
        bool normalOrEffect = ygoType == YgoCardType.Monster || ygoType == YgoCardType.EffectMonster;
        bool ritual = ygoType == YgoCardType.RitualMonster;
        bool fusion = ygoType == YgoCardType.FusionMonster;

        if (level <= 4)
        {
            if (ygoType == YgoCardType.Monster)
                return UpgradedCostNormalLow4(costCol, matchStat);
            if (ygoType == YgoCardType.EffectMonster)
                return null;
            if (fusion && matchStat < 19)
                return UpgradedCostFusionUnifiedLow19(costCol, matchStat);
            if (ritual || fusion)
                return UpgradedCostRitualFusionLow4(costCol, matchStat);
        }

        if (level <= 6)
        {
            if (normalOrEffect)
                return UpgradedCostNormalEffect56(costCol, matchStat);
            if (fusion && matchStat < 19)
                return UpgradedCostFusionUnifiedMid19(costCol, matchStat);
            if (ritual || fusion)
                return UpgradedCostRitualFusion56(costCol, matchStat);
        }

        if (level <= 8)
        {
            if (fusion && matchStat < 19)
                return UpgradedCostFusionUnifiedHigh19(costCol, matchStat);
            if (fusion)
                return UpgradedCostFusion78(costCol, matchStat);
            if (normalOrEffect || ritual)
                return UpgradedCostNormalRitualEffect78(costCol, matchStat);
        }

        if (level <= 10)
            return UpgradedCostAll910(costCol, matchStat);

        return UpgradedCostAll11Plus(costCol, matchStat);
    }

    private static int? UpgradedCostNormalLow4(int costCol, int s)
    {
        if (costCol == 1 && s == 3) return 0;
        if (costCol == 1 && s == 4) return 0;
        if (costCol == 1 && s == 7) return 1;
        if (costCol == 1 && s == 8) return 1;
        if (costCol == 2 && s == 9) return 1;
        if (costCol == 2 && s >= 10 && s <= 12) return 1;
        if (costCol == 2 && s >= 13 && s <= 14) return 2;
        if (costCol == 2 && s >= 15 && s <= 16) return 2;
        if (costCol == 2 && s == 17) return 2;
        if (costCol == 2 && s >= 18 && s <= 21) return 2;
        if (costCol == 2 && s >= 22) return 2;
        return null;
    }

    /// <summary>Fusion monsters L1–8, printed Z &lt; 19: upgraded play energy (smith) per Fusion_Monster_Balancing / user spec.</summary>
    private static int? UpgradedCostFusionUnifiedLow19(int costCol, int s)
    {
        if (costCol == 1 && s <= 10) return 0;
        if (costCol == 1 && s >= 11 && s <= 12) return 0;
        if (costCol == 2 && s >= 13 && s <= 16) return 1;
        if (costCol == 2 && s >= 17 && s <= 18) return 2;
        return null;
    }

    private static int? UpgradedCostRitualFusionLow4(int costCol, int s)
    {
        if (costCol == 1 && s == 11) return 1;
        if (costCol == 1 && s == 12) return 1;
        if (costCol == 2 && s == 13) return 1;
        if (costCol == 2 && s >= 14 && s <= 16) return 1;
        if (costCol == 2 && s >= 17 && s <= 18) return 2;
        if (costCol == 2 && s >= 19 && s <= 20) return 2;
        if (costCol == 2 && s == 21) return 2;
        if (costCol == 2 && s >= 22 && s <= 25) return 2;
        if (costCol == 2 && s >= 26) return 2;
        return null;
    }

    private static int? UpgradedCostNormalEffect56(int costCol, int s)
    {
        if (costCol == 1 && s == 12) return 1;
        if (costCol == 1 && s == 13) return 1;
        if (costCol == 2 && s == 14) return 1;
        if (costCol == 2 && s >= 15 && s <= 17) return 1;
        if (costCol == 2 && s >= 18 && s <= 19) return 2;
        if (costCol == 2 && s >= 20 && s <= 21) return 2;
        if (costCol == 2 && s == 22) return 2;
        if (costCol == 2 && s >= 23 && s <= 26) return 2;
        if (costCol == 2 && s >= 27) return 2;
        return null;
    }

    /// <summary>Fusion monsters L1–8, printed Z &lt; 19: upgraded play energy (smith) per Fusion_Monster_Balancing / user spec.</summary>
    private static int? UpgradedCostFusionUnifiedMid19(int costCol, int s)
    {
        if (costCol == 1 && s <= 10) return 0;
        if (costCol == 1 && s >= 11 && s <= 12) return 0;
        if (costCol == 2 && s >= 13 && s <= 18) return 1;
        return null;
    }

    private static int? UpgradedCostRitualFusion56(int costCol, int s)
    {
        if (costCol == 1 && s == 13) return 1;
        if (costCol == 1 && s == 14) return 1;
        if (costCol == 2 && s == 15) return 1;
        if (costCol == 2 && s >= 16 && s <= 18) return 1;
        if (costCol == 2 && s >= 19 && s <= 20) return 2;
        if (costCol == 2 && s >= 21 && s <= 22) return 2;
        if (costCol == 2 && s == 23) return 2;
        if (costCol == 2 && s >= 24 && s <= 27) return 2;
        if (costCol == 2 && s >= 28) return 2;
        return null;
    }

    private static int? UpgradedCostNormalRitualEffect78(int costCol, int s)
    {
        if (costCol == 1 && s == 16) return 1;
        if (costCol == 1 && s == 17) return 1;
        if (costCol == 2 && s == 18) return 1;
        if (costCol == 2 && s >= 19 && s <= 21) return 1;
        if (costCol == 2 && s >= 22 && s <= 23) return 2;
        if (costCol == 2 && s >= 24 && s <= 25) return 2;
        if (costCol == 2 && s == 26) return 2;
        if (costCol == 2 && s >= 27 && s <= 30) return 2;
        if (costCol == 2 && s >= 31) return 2;
        return null;
    }

    /// <summary>Fusion monsters L1–8, printed Z &lt; 19: upgraded play energy (smith) per Fusion_Monster_Balancing / user spec.</summary>
    private static int? UpgradedCostFusionUnifiedHigh19(int costCol, int s)
    {
        if (costCol == 1 && s <= 10) return 0;
        if (costCol == 1 && s >= 11 && s <= 12) return 0;
        if (costCol == 2 && s >= 13 && s <= 18) return 1;
        return null;
    }

    private static int? UpgradedCostFusion78(int costCol, int s)
    {
        if (costCol == 1 && s == 18) return 1;
        if (costCol == 1 && s == 19) return 1;
        if (costCol == 2 && s == 20) return 1;
        if (costCol == 2 && s >= 21 && s <= 23) return 1;
        if (costCol == 2 && s >= 24 && s <= 25) return 2;
        if (costCol == 2 && s >= 26 && s <= 27) return 2;
        if (costCol == 2 && s == 28) return 2;
        if (costCol == 2 && s >= 29 && s <= 32) return 2;
        if (costCol == 2 && s >= 33) return 2;
        return null;
    }

    private static int? UpgradedCostAll910(int costCol, int s)
    {
        if (costCol == 1 && s == 25) return 1;
        if (costCol == 1 && s == 26) return 1;
        if (costCol == 2 && s == 27) return 1;
        if (costCol == 2 && s >= 28 && s <= 30) return 1;
        if (costCol == 2 && s >= 31 && s <= 32) return 2;
        if (costCol == 2 && s >= 33 && s <= 34) return 2;
        if (costCol == 2 && s == 35) return 2;
        if (costCol == 2 && s >= 36 && s <= 39) return 2;
        if (costCol == 2 && s >= 40) return 2;
        return null;
    }

    private static int? UpgradedCostAll11Plus(int costCol, int s)
    {
        if (costCol == 1 && s == 29) return 1;
        if (costCol == 1 && s == 30) return 1;
        if (costCol == 2 && s == 31) return 1;
        if (costCol == 2 && s >= 32 && s <= 34) return 1;
        if (costCol == 2 && s >= 35 && s <= 36) return 2;
        if (costCol == 2 && s >= 37 && s <= 38) return 2;
        if (costCol == 2 && s == 39) return 2;
        if (costCol == 2 && s >= 40 && s <= 43) return 2;
        if (costCol == 2 && s >= 44) return 2;
        return null;
    }

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
    /// First smith upgrade on a printed ATK, DEF, or MGC line. <paramref name="baseStatLine"/> is the unupgraded printed value on that line.
    /// Uses the same smith stat row for ATK and DEF at the same number (e.g. 3/3 → +2/+2). The DEF−1 band shift applies to
    /// play-energy / cost lookups (<see cref="TryGetSmithUpgradedPlayEnergy"/>), not to this stat delta.
    /// Normal L≤4: set <paramref name="isDefenseLine"/> true for DEF so 2-cost printed DEF 8/9 upgrade by +5/+4 to 13 (paired with 1-cost upgraded play energy).
    /// </summary>
    public static int GetMonsterPrintedLineUpgradeDelta(
        int level,
        YgoCardType ygoType,
        int unupgradedPlayEnergyForLine,
        int baseStatLine,
        bool isDefenseLine = false)
    {
        if (ygoType == YgoCardType.EffectMonster && level <= 4)
            return GetLegacyMonsterSmithDelta(baseStatLine);

        int costCol = unupgradedPlayEnergyForLine <= 1 ? 1 : 2;
        int matchStat = baseStatLine;

        bool fusion = ygoType == YgoCardType.FusionMonster;
        bool ritual = ygoType == YgoCardType.RitualMonster;
        bool normalOrEffect = ygoType == YgoCardType.Monster || ygoType == YgoCardType.EffectMonster;

        if (level <= 4)
        {
            if (normalOrEffect)
            {
                if (ygoType == YgoCardType.Monster && isDefenseLine && costCol == 2 && (baseStatLine == 8 || baseStatLine == 9))
                    return baseStatLine == 8 ? 5 : 4;
                return SmithNormalLow4(costCol, matchStat);
            }
            if (fusion && matchStat < 19)
                return SmithFusionStatLow19(costCol, matchStat);
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
        int e = MonsterEnergyCostCalculator.GetUnupgradedPlayEnergyFromZBand(level, ygoType, baseMgc, true, statsUnknown);
        return GetMonsterPrintedLineUpgradeDelta(level, ygoType, e, baseMgc);
    }

    private static int SmithNormalLow4(int costCol, int s)
    {
        if (costCol == 1 && s == 4) return 3;
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
    
    /// <summary>Fusion L1–8 first-upgrade ATK/DEF deltas for printed stat &lt; 19 (same row for ATK and DEF).</summary>
    private static int SmithFusionStatLow19(int costCol, int s)
    {
        if (costCol == 1 && s == 11) return 3;
        if (costCol == 1 && s == 12) return 2;
        if (costCol == 1 && s == 13) return 4;
        if (costCol == 2 && s == 14) return 5;
        if (costCol == 2 && s == 15) return 4;
        if (costCol == 2 && s == 16) return 4;
        if (costCol == 2 && s == 17) return 4;
        if (costCol == 2 && s == 18) return 3;
        return SmithRitualFusionLow4(costCol,s);
    }

    private static int SmithNormalEffect56(int costCol, int s)
    {
        if (costCol == 1 && s == 12) return 3;
        if (costCol == 1 && s == 13) return 3;
        if (costCol == 2 && s == 14) return 4;
        if (costCol == 2 && s >= 15 && s <= 17) return 17 - s;
        if (costCol == 2 && s >= 18 && s <= 19) return 22 - s;
        if (costCol == 2 && s >= 20 && s <= 21) return 25 - s;
        if (costCol == 2 && s == 22) return 4;
        if (costCol == 2 && s >= 23 && s <= 26) return 4;
        if (costCol == 2 && s >= 27) return 5;
        return GetLegacyMonsterSmithDelta(s);
    }

    private static int SmithRitualFusion56(int costCol, int s)
    {
        if (costCol == 1 && s == 11) return 3;
        if (costCol == 1 && s == 12) return 2;
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
        return GetLegacyMonsterSmithDelta(s) + 1;
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
        return SmithRitualFusion56(costCol, s) + 1;
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
        return GetLegacyMonsterSmithDelta(s) + 1;
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
        return GetLegacyMonsterSmithDelta(s) + 1;
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
