using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Z = printed ATK (attack stance) or DEF (defense stance) before smith. Maps level, monster kind, and Z to play energy 0–3;
/// upgraded preview uses the same bands as <c>Monster_Stat_Scaling_Balanced.md</c>.
/// </summary>
public static class MonsterEnergyCostCalculator
{
    /// <summary>Play / command energy for one stance. Unknown stats → 1 (design doc).</summary>
    public static int GetMonsterPlayEnergy(
        int level,
        YgoCardType ygoType,
        int z,
        bool isAttackStat,
        bool upgradedOrPreview,
        bool statsUnknown)
    {
        if (statsUnknown || z < 0)
            return 1;

        bool normalOrEffect = ygoType == YgoCardType.Monster || ygoType == YgoCardType.EffectMonster;
        bool ritual = ygoType == YgoCardType.RitualMonster;
        bool fusion = ygoType == YgoCardType.FusionMonster;

        if (level <= 4)
        {
            if (normalOrEffect)
                return NormalEffectLevel1To4(z, isAttackStat, upgradedOrPreview);
            if (ritual || fusion)
                return RitualFusionLevel1To4(z, isAttackStat, upgradedOrPreview);
        }

        if (level <= 6)
        {
            if (normalOrEffect)
                return NormalEffectLevel5To6(z, isAttackStat, upgradedOrPreview);
            if (ritual || fusion)
                return RitualFusionLevel5To6(z, isAttackStat, upgradedOrPreview);
        }

        if (level <= 8)
        {
            if (fusion)
                return FusionLevel7To8(z, isAttackStat, upgradedOrPreview);
            if (normalOrEffect || ritual)
                return NormalRitualEffectLevel7To8(z, isAttackStat, upgradedOrPreview);
        }

        if (level <= 10)
            return Level9To10(z, isAttackStat, upgradedOrPreview);

        return Level11Plus(z, isAttackStat, upgradedOrPreview);
    }

    private static int NormalEffectLevel1To4(int z, bool isAttackStat, bool up)
    {
        if (isAttackStat)
        {
            if (z <= 3) return 0;
            if (z <= 8) return 1;
            if (z <= 12) return up ? 1 : 2;
            if (z <= 20) return 2;
            return 3;
        }

        if (z <= 3) return 0;
        if (z <= 7) return 1;
        if (z == 8) return up ? 1 : 2;
        if (z <= 12) return up ? 1 : 2;
        if (z <= 20) return 2;
        return 3;
    }

    private static int RitualFusionLevel1To4(int z, bool isAttackStat, bool up)
    {
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

    private static int NormalEffectLevel5To6(int z, bool isAttackStat, bool up)
    {
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

    private static int RitualFusionLevel5To6(int z, bool isAttackStat, bool up)
    {
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

    private static int NormalRitualEffectLevel7To8(int z, bool isAttackStat, bool up)
    {
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

    private static int FusionLevel7To8(int z, bool isAttackStat, bool up)
    {
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

    private static int Level9To10(int z, bool isAttackStat, bool up)
    {
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

    private static int Level11Plus(int z, bool isAttackStat, bool up)
    {
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
