using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>All custom card-library sidebar filters for one compendium instance.</summary>
public sealed class YgoCardLibrarySidebarFilterState
{
    public YgoCardLibraryPackTagFilterState PackTags { get; } = new();

    public YgoCardLibraryYgoCardTypeFilterState YgoCardTypes { get; } = new();

    public YgoCardLibraryLevelFilterState Level { get; } = new();

    public YgoCardLibraryAttributeFilterState Attribute { get; } = new();

    public YgoCardLibraryRaceFilterState Race { get; } = new();

    public YgoCardLibraryStatRangeFilterState Atk { get; } = new();

    public YgoCardLibraryStatRangeFilterState Def { get; } = new();

    /// <summary>Last-used ATK/DEF sort header; drives the extra compendium grid reorder pass.</summary>
    public YgoCardLibraryMonsterStatSortAxis PrimaryMonsterStatSort { get; set; }

    public bool Matches(CardModel card) =>
        PackTags.Matches(card) && YgoCardTypes.Matches(card) && Level.Matches(card) && Attribute.Matches(card)
        && Race.Matches(card)
        && Atk.Matches(card, bm => bm.BaseAtk)
        && Def.Matches(card, bm => bm.BaseDef);

    public void ResetToDefaults()
    {
        PrimaryMonsterStatSort = YgoCardLibraryMonsterStatSortAxis.None;
        PackTags.ResetToDefaults();
        YgoCardTypes.ResetToDefaults();
        Level.ResetToDefaults();
        Attribute.ResetToDefaults();
        Race.ResetToDefaults();
        Atk.ResetToDefaults();
        Def.ResetToDefaults();
    }
}
