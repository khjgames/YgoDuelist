using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Play energy: (1) <b>Unupgraded</b> cards — Z-band tables from <c>Monster_Stat_Scaling_Balanced.md</c> using printed Z before smith.
/// (2) <b>Upgraded / preview</b> — smith table <b>Upgraded cost</b> only, keyed by <b>unupgraded</b> constructor stats (never Z-band on post-upgrade ATK/DEF).
/// Effect L≤4 uses legacy upgrade-annotation rows with unupgraded Z only.
/// </summary>
public static class MonsterEnergyCostCalculator
{
    /// <param name="z">Printed ATK or DEF for this stance. When <paramref name="upgradedOrPreview"/> is false, this is unupgraded Z. When true, pass-through for legacy paths; smith uses <paramref name="constructorPrintedStatForThisStanceLine"/> only.</param>
    /// <param name="constructorPrintedStatForThisStanceLine">Unupgraded BaseAtk or BaseDef for this line; required for smith upgraded cost. Use <c>-1</c> to skip (e.g. MGC-as-Z).</param>
    public static int GetMonsterPlayEnergy(
        int level,
        YgoCardType ygoType,
        int z,
        bool isAttackStat,
        bool upgradedOrPreview,
        bool statsUnknown,
        int constructorPrintedStatForThisStanceLine = -1)
    {
        if (statsUnknown || z < 0)
            return 1;

        if (upgradedOrPreview && constructorPrintedStatForThisStanceLine >= 0)
        {
            if (YgoStatUpgradeScaling.TryGetSmithUpgradedPlayEnergy(
                    level,
                    ygoType,
                    isDefenseLine: !isAttackStat,
                    constructorPrintedStatForThisStanceLine,
                    statsUnknown,
                    out int smithEnergy))
                return smithEnergy;

            if (ygoType == YgoCardType.EffectMonster && level <= 4)
                return ZBandEffectLow4UpgradeAnnotationsOnly(constructorPrintedStatForThisStanceLine, isAttackStat);

            // Missing smith row: never use post-upgrade Z for bands — stay on unupgraded constructor stat.
            return ZBandUnupgradedEnergy(level, ygoType, constructorPrintedStatForThisStanceLine, isAttackStat);
        }

        return ZBandUnupgradedEnergy(level, ygoType, z, isAttackStat);
    }

    /// <summary>Unupgraded play energy from Z-bands only (<c>upgradedOrPreview</c> is always false for table semantics).</summary>
    internal static int GetUnupgradedPlayEnergyFromZBand(
        int level,
        YgoCardType ygoType,
        int unupgradedZ,
        bool isAttackStat,
        bool statsUnknown)
    {
        if (statsUnknown || unupgradedZ < 0)
            return 1;
        return ZBandUnupgradedEnergy(level, ygoType, unupgradedZ, isAttackStat);
    }

    private static int ZBandUnupgradedEnergy(int level, YgoCardType ygoType, int z, bool isAttackStat)
    {
        bool normalOrEffect = ygoType == YgoCardType.Monster || ygoType == YgoCardType.EffectMonster;
        bool ritual = ygoType == YgoCardType.RitualMonster;
        bool fusion = ygoType == YgoCardType.FusionMonster;

        if (level <= 4)
        {
            if (normalOrEffect)
                return NormalEffectLevel1To4(z, isAttackStat, upgradeAnnotations: false);
            if (ritual || fusion)
                return RitualFusionLevel1To4(z, isAttackStat, upgradeAnnotations: false);
        }

        if (level <= 6)
        {
            if (normalOrEffect)
                return NormalEffectLevel5To6(z, isAttackStat, upgradeAnnotations: false);
            if (ritual || fusion)
                return RitualFusionLevel5To6(z, isAttackStat, upgradeAnnotations: false);
        }

        if (level <= 8)
        {
            if (fusion)
                return FusionLevel7To8(z, isAttackStat, upgradeAnnotations: false);
            if (normalOrEffect || ritual)
                return NormalRitualEffectLevel7To8(z, isAttackStat, upgradeAnnotations: false);
        }

        if (level <= 10)
            return Level9To10(z, isAttackStat, upgradeAnnotations: false);

        return Level11Plus(z, isAttackStat, upgradeAnnotations: false);
    }

    /// <summary>Effect L≤4 legacy: doc “onUpgrade” notes; keyed by <b>unupgraded</b> Z only.</summary>
    private static int ZBandEffectLow4UpgradeAnnotationsOnly(int unupgradedZ, bool isAttackStat) =>
        NormalEffectLevel1To4(unupgradedZ, isAttackStat, upgradeAnnotations: true);

    private static int NormalEffectLevel1To4(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 3) return 0;
            if (z == 4) return up ? 0 : 1;
            if (z <= 7) return 1;
            if (z == 8) return 1;
            if (z <= 12) return up ? 1 : 2;
            if (z <= 20) return 2;
            return 3;
        }

        if (z <= 3) return 0;
        if (z == 4) return up ? 0 : 1;
        if (z <= 7) return 1;
        if (z == 8) return up ? 1 : 2;
        if (z <= 12) return up ? 1 : 2;
        if (z <= 20) return 2;
        return 3;
    }

    private static int RitualFusionLevel1To4(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 7) return 0;
            if (z <= 11) return 1;
            if (z <= 16) return up ? 1 : 2;
            if (z <= 24) return 2;
            return 3;
        }

        if (z <= 7) return 0;
        if (z <= 11) return 1;
        if (z == 12) return up ? 1 : 2;
        if (z <= 16) return up ? 1 : 2;
        if (z <= 24) return 2;
        return 3;
    }

    private static int NormalEffectLevel5To6(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 8) return 0;
            if (z <= 12) return 1;
            if (z == 13) return 1;
            if (z <= 17) return up ? 1 : 2;
            if (z <= 25) return 2;
            return 3;
        }

        if (z <= 8) return 0;
        if (z <= 12) return 1;
        if (z == 13) return up ? 1 : 2;
        if (z <= 17) return up ? 1 : 2;
        if (z <= 25) return 2;
        return 3;
    }

    private static int RitualFusionLevel5To6(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 9) return 0;
            if (z <= 13) return 1;
            if (z == 14) return 1;
            if (z <= 18) return up ? 1 : 2;
            if (z <= 27) return 2;
            return 3;
        }

        if (z <= 9) return 0;
        if (z <= 13) return 1;
        if (z == 14) return up ? 1 : 2;
        if (z <= 18) return up ? 1 : 2;
        if (z <= 27) return 2;
        return 3;
    }

    private static int NormalRitualEffectLevel7To8(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 10) return 0;
            if (z <= 16) return 1;
            if (z == 17) return 1;
            if (z <= 22) return up ? 1 : 2;
            if (z <= 30) return 2;
            return 3;
        }

        if (z <= 10) return 0;
        if (z <= 16) return 1;
        if (z == 17) return up ? 1 : 2;
        if (z <= 22) return up ? 1 : 2;
        if (z <= 30) return 2;
        return 3;
    }

    private static int FusionLevel7To8(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 11) return 0;
            if (z <= 18) return 1;
            if (z == 19) return 1;
            if (z <= 24) return up ? 1 : 2;
            if (z <= 31) return 2;
            return 3;
        }

        if (z <= 11) return 0;
        if (z <= 18) return 1;
        if (z == 19) return up ? 1 : 2;
        if (z <= 24) return up ? 1 : 2;
        if (z <= 31) return 2;
        return 3;
    }

    private static int Level9To10(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 11) return 0;
            if (z <= 25) return 1;
            if (z == 26) return 1;
            if (z <= 32) return up ? 1 : 2;
            if (z <= 45) return 2;
            return 3;
        }

        if (z <= 11) return 0;
        if (z <= 25) return 1;
        if (z == 26) return up ? 1 : 2;
        if (z <= 32) return up ? 1 : 2;
        if (z <= 45) return 2;
        return 3;
    }

    private static int Level11Plus(int z, bool isAttackStat, bool upgradeAnnotations)
    {
        bool up = upgradeAnnotations;
        if (isAttackStat)
        {
            if (z <= 12) return 0;
            if (z <= 30) return 1;
            if (z <= 35) return up ? 1 : 2;
            if (z <= 60) return 2;
            return 3;
        }

        if (z <= 12) return 0;
        if (z <= 30) return 1;
        if (z <= 35) return up ? 1 : 2;
        if (z <= 60) return 2;
        return 3;
    }
}
