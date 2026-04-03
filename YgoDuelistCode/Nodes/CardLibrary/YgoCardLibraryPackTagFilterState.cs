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

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? NoneToggle { get; set; }

    public List<(YgoCardPackTags Flag, CardLibraryFilterSortingRuleCategoryFilterToggleGUI Gui)> FlagToggles { get; } = new();

    public bool Matches(CardModel card)
    {
        YgoCardPackTags tags = card is YgoDuelistCard y ? y.PackTags : YgoCardPackTags.None;
        var includeParts = new List<Func<bool>>();
        var excludeParts = new List<Func<bool>>();

        if (NoneToggle != null)
        {
            switch (NoneToggle.RowState)
            {
                case CardLibraryFilterTriState.Include:
                    includeParts.Add(() => tags == YgoCardPackTags.None);
                    break;
                case CardLibraryFilterTriState.Exclude:
                    excludeParts.Add(() => tags == YgoCardPackTags.None);
                    break;
            }
        }

        foreach ((YgoCardPackTags flag, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in FlagToggles)
        {
            switch (gui.RowState)
            {
                case CardLibraryFilterTriState.Include:
                {
                    YgoCardPackTags captured = flag;
                    includeParts.Add(() => (tags & captured) != 0);
                    break;
                }
                case CardLibraryFilterTriState.Exclude:
                {
                    YgoCardPackTags captured = flag;
                    excludeParts.Add(() => (tags & captured) != 0);
                    break;
                }
            }
        }

        foreach (Func<bool> p in excludeParts)
        {
            if (p())
                return false;
        }

        if (includeParts.Count == 0)
            return true;

        foreach (Func<bool> p in includeParts)
        {
            if (p())
                return true;
        }

        return false;
    }

    public void ResetToDefaults()
    {
        if (NoneToggle != null)
            NoneToggle.RowState = CardLibraryFilterTriState.Neutral;
        foreach ((_, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in FlagToggles)
            gui.RowState = CardLibraryFilterTriState.Neutral;
    }
}
