using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per-tag desire multipliers (neutral 10). Tuned for pack theme balance; see Clarifying_Weighted_Tag_Selection.md.
/// </summary>
public static class YgoPackTagWeightConfig
{
    public const int NeutralMultiplier = 10;

    public static int GetWeightMultiplier(YgoCardPackTags singleBitFlag)
    {
        return singleBitFlag switch
        {
            YgoCardPackTags.Spell => 22,
            YgoCardPackTags.Trap => 24,
            YgoCardPackTags.Earth or YgoCardPackTags.Water or YgoCardPackTags.Wind
                or YgoCardPackTags.Fire or YgoCardPackTags.Dark or YgoCardPackTags.Light => 8,
            YgoCardPackTags.Dragon or YgoCardPackTags.Ocean or YgoCardPackTags.Insect
                or YgoCardPackTags.Machine or YgoCardPackTags.Zombie or YgoCardPackTags.Fiend
                or YgoCardPackTags.Spellcaster or YgoCardPackTags.Warrior => 12,
            _ => NeutralMultiplier
        };
    }
}
