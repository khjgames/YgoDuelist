using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>All custom card-library sidebar filters for one compendium instance.</summary>
public sealed class YgoCardLibrarySidebarFilterState
{
    public YgoCardLibraryPackTagFilterState PackTags { get; } = new();

    public YgoCardLibraryArchetypeFilterState Archetypes { get; } = new();

    public YgoCardLibraryYgoCardTypeFilterState YgoCardTypes { get; } = new();

    public YgoCardLibraryLevelFilterState Level { get; } = new();

    public YgoCardLibraryAttributeFilterState Attribute { get; } = new();

    public YgoCardLibraryRaceFilterState Race { get; } = new();

    public YgoCardLibraryStatRangeFilterState Atk { get; } = new();

    public YgoCardLibraryStatRangeFilterState Def { get; } = new();

    public YgoCardLibraryPackWeightRangeFilterState PackWeight { get; } = new();

    /// <summary>Last-used pack weight / ATK / DEF sort header; drives the extra compendium grid reorder pass.</summary>
    public YgoCardLibraryMonsterStatSortAxis PrimaryMonsterStatSort { get; set; }

    public bool Matches(CardModel card) =>
        PackTags.Matches(card) && Archetypes.Matches(card) && YgoCardTypes.Matches(card) && Level.Matches(card)
        && Attribute.Matches(card)
        && Race.Matches(card)
        && PackWeight.Matches(card)
        && Atk.Matches(card, bm => bm.BaseAtk)
        && Def.Matches(card, bm => bm.BaseDef);

    public void ResetToDefaults()
    {
        PrimaryMonsterStatSort = YgoCardLibraryMonsterStatSortAxis.None;
        PackTags.ResetToDefaults();
        Archetypes.ResetToDefaults();
        YgoCardTypes.ResetToDefaults();
        Level.ResetToDefaults();
        Attribute.ResetToDefaults();
        Race.ResetToDefaults();
        PackWeight.ResetToDefaults();
        Atk.ResetToDefaults();
        Def.ResetToDefaults();
    }
}
