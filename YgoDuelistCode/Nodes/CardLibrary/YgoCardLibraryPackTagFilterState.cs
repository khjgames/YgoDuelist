using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Compendium pack-tag filter row state for one <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary"/> instance.</summary>
public sealed class YgoCardLibraryPackTagFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? AnyToggle { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? NoneToggle { get; set; }

    public List<(YgoCardPackTags Flag, CardLibraryFilterSortingRuleCategoryFilterToggleGUI Gui)> FlagToggles { get; } = new();

    public bool Matches(CardModel card)
    {
        YgoCardPackTags tags = card is YgoDuelistCard y ? y.PackTags : YgoCardPackTags.None;
        var parts = new List<Func<bool>>();

        if (AnyToggle?.IsTicked == true)
            parts.Add(() => tags != YgoCardPackTags.None);

        if (NoneToggle?.IsTicked == true)
            parts.Add(() => tags == YgoCardPackTags.None);

        foreach ((YgoCardPackTags flag, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in FlagToggles)
        {
            if (gui.IsTicked)
            {
                YgoCardPackTags captured = flag;
                parts.Add(() => (tags & captured) != 0);
            }
        }

        if (parts.Count == 0)
            return true;

        foreach (Func<bool> p in parts)
        {
            if (p())
                return true;
        }

        return false;
    }

    public void ResetToDefaults()
    {
        if (AnyToggle != null)
            AnyToggle.IsTicked = true;
        if (NoneToggle != null)
            NoneToggle.IsTicked = true;
        foreach ((_, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in FlagToggles)
            gui.IsTicked = false;
    }
}
