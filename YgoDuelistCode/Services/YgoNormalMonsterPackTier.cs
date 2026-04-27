namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Combined tier (0.4–2) for normal monster pack weighting from printed level/ATK/DEF
/// (<see cref="Cards.Core.NormalMonsterCard.PackWeightMultiplier"/>, before final adjustment).
/// <c>max(ATK,DEF)*0.8 + min(ATK,DEF)*0.2</c> on scored efficiencies, then clamp.
/// </summary>
public static class YgoNormalMonsterPackTier
{
    public static float ComputeCombinedTier(int duelMonsterLevel, int atk, int def)
    {
        int lv = duelMonsterLevel < 1 ? 1 : duelMonsterLevel;

        float atkEff = lv switch
        {
            <= 4 => LowLevelAtkEfficiency(atk),
            <= 6 => MediumLevelAtkEfficiency(atk),
            _ => HighLevelAtkEfficiency(atk)
        };

        float defEff = lv switch
        {
            <= 4 => LowLevelDefEfficiency(def),
            <= 6 => MediumLevelDefEfficiency(def),
            _ => HighLevelDefEfficiency(def)
        };

        float primaryEff = MathF.Max(atkEff, defEff) * 0.75f;
        float secondaryEff = MathF.Min(atkEff, defEff) * 0.25f;
        float combined = primaryEff + secondaryEff;
        if (combined < 0.4f)
            return 0.4f;
        if (combined > 2f)
            return 2f;
        return combined;
    }

    static float LowLevelAtkEfficiency(int atk)
    {
        return atk switch
        {
            0 => 0.7f,
            1 => 0.85f,
            2 => 1f,
            3 => 1.25f,
            4 => 1.1f,
            5 => 0.6f,
            6 => 0.8f,
            7 => 0.95f,
            8 => 1.1f,
            9 => 1.05f,
            10 => 0.8f,
            11 => 0.8f,
            12 => 0.9f,
            13 => 0.6f,
            14 => 0.6f,
            15 => 0.65f,
            16 => 0.7f,
            17 => 0.75f,
            18 => 0.85f,
            19 => 1f,
            _ => 1f
        };
    }

    static float LowLevelDefEfficiency(int def)
    {
        return def switch
        {
            0 => 0.6f,
            1 => 0.75f,
            2 => 0.9f,
            3 => 1.2f,
            4 => 1.2f,
            5 => 0.5f,
            6 => 0.9f,
            7 => 1.15f,
            8 => 1.1f,
            9 => 0.75f,
            10 => 0.75f,
            11 => 0.85f,
            12 => 0.55f,
            13 => 0.6f,
            14 => 0.65f,
            15 => 0.7f,
            16 => 0.75f,
            17 => 0.85f,
            18 => 0.9f,
            19 => 1f,
            20 => 1.15f,
            21 => 1.25f,
            _ => 1f
        };
    }

    static float MediumLevelAtkEfficiency(int atk)
    {
        return atk switch
        {
            0 => 0.4f,
            1 => 0.45f,
            2 => 0.5f,
            3 => 0.55f,
            4 => 0.6f,
            5 => 0.75f,
            6 => 0.85f,
            7 => 1f,
            8 => 1.3f,
            9 => 0.6f,
            10 => 0.7f,
            11 => 0.8f,
            12 => 0.9f,
            13 => 1.05f,
            14 => 1.25f,
            15 => 1.05f,
            16 => 1.05f,
            17 => 1.15f,
            18 => 0.6f,
            19 => 0.65f,
            20 => 0.7f,
            21 => 0.75f,
            22 => 0.85f,
            23 => 0.95f,
            24 => 1.05f,
            25 => 1.15f,
            26 => 1.25f,
            _ => 1f
        };
    }

    static float MediumLevelDefEfficiency(int def)
    {
        return def switch
        {
            0 => 0.4f,
            1 => 0.45f,
            2 => 0.5f,
            3 => 0.55f,
            4 => 0.6f,
            5 => 0.75f,
            6 => 0.85f,
            7 => 1f,
            8 => 0.6f,
            9 => 0.7f,
            10 => 0.8f,
            11 => 0.9f,
            12 => 1.05f,
            13 => 1.25f,
            14 => 1.05f,
            15 => 1.05f,
            16 => 1.15f,
            17 => 0.6f,
            18 => 0.65f,
            19 => 0.7f,
            20 => 0.75f,
            21 => 0.85f,
            22 => 0.95f,
            23 => 1.05f,
            24 => 1.15f,
            25 => 1.25f,
            26 => 1.35f,
            27 => 1.45f,
            28 => 1.55f,
            29 => 1.65f,
            30 => 1.75f,
            _ => 1f
        };
    }

    static float HighLevelAtkEfficiency(int atk)
    {
        return atk switch
        {
            0 => 0.3f,
            1 => 0.35f,
            2 => 0.4f,
            3 => 0.45f,
            4 => 0.5f,
            5 => 0.55f,
            6 => 0.7f,
            7 => 0.8f,
            8 => 1.05f,
            9 => 1.2f,
            10 => 1.35f,
            11 => 0.5f,
            12 => 0.6f,
            13 => 0.7f,
            14 => 0.8f,
            15 => 0.9f,
            16 => 1f,
            17 => 1.15f,
            18 => 1.35f,
            19 => 1.1f,
            20 => 1.1f,
            21 => 1.2f,
            22 => 0.65f,
            23 => 0.7f,
            24 => 0.75f,
            25 => 0.85f,
            26 => 0.95f,
            27 => 1.05f,
            28 => 1.15f,
            29 => 1.25f,
            30 => 1.35f,
            _ => 1f
        };
    }

    static float HighLevelDefEfficiency(int def)
    {
        return def switch
        {
            0 => 0.35f,
            1 => 0.4f,
            2 => 0.45f,
            3 => 0.5f,
            4 => 0.55f,
            5 => 0.7f,
            6 => 0.8f,
            7 => 1.05f,
            8 => 1.2f,
            9 => 1.35f,
            10 => 0.5f,
            11 => 0.6f,
            12 => 0.7f,
            13 => 0.8f,
            14 => 0.9f,
            15 => 1f,
            16 => 1.15f,
            17 => 1.35f,
            18 => 1.1f,
            19 => 1.1f,
            20 => 1.2f,
            21 => 0.65f,
            22 => 0.7f,
            23 => 0.75f,
            24 => 0.85f,
            25 => 0.95f,
            26 => 1.05f,
            27 => 1.15f,
            28 => 1.25f,
            29 => 1.35f,
            30 => 1.45f,
            _ => 1f
        };
    }
}
