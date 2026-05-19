using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Compendium archetype filter row state for one <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary"/> instance.</summary>
public sealed class YgoCardLibraryArchetypeFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? NoneToggle { get; set; }

    public List<(YgoCardArchetype Flag, CardLibraryFilterSortingRuleCategoryFilterToggleGUI Gui)> FlagToggles { get; } = new();

    public static YgoCardArchetype GetEffectiveArchetypes(CardModel card)
    {
        if (card is not YgoDuelistCard yd)
            return YgoCardArchetype.None;
        return yd.CardArchetypes | YgoCardArchetypeRegistry.GetImplicitArchetypes(card.GetType());
    }

    public bool Matches(CardModel card)
    {
        YgoCardArchetype declared = card is YgoDuelistCard yd ? yd.CardArchetypes : YgoCardArchetype.None;
        YgoCardArchetype effective = GetEffectiveArchetypes(card);
        var includeOrParts = new List<Func<bool>>();
        var requireAndParts = new List<Func<bool>>();
        var excludeParts = new List<Func<bool>>();

        if (NoneToggle != null)
        {
            switch (NoneToggle.RowState)
            {
                case CardLibraryFilterTriState.Include:
                    includeOrParts.Add(() => declared == YgoCardArchetype.None);
                    break;
                case CardLibraryFilterTriState.RequireAnd:
                    requireAndParts.Add(() => declared == YgoCardArchetype.None);
                    break;
                case CardLibraryFilterTriState.Exclude:
                    excludeParts.Add(() => declared == YgoCardArchetype.None);
                    break;
            }
        }

        foreach ((YgoCardArchetype flag, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in FlagToggles)
        {
            switch (gui.RowState)
            {
                case CardLibraryFilterTriState.Include:
                {
                    YgoCardArchetype captured = flag;
                    includeOrParts.Add(() => (effective & captured) != 0);
                    break;
                }
                case CardLibraryFilterTriState.RequireAnd:
                {
                    YgoCardArchetype captured = flag;
                    requireAndParts.Add(() => (effective & captured) != 0);
                    break;
                }
                case CardLibraryFilterTriState.Exclude:
                {
                    YgoCardArchetype captured = flag;
                    excludeParts.Add(() => (effective & captured) != 0);
                    break;
                }
            }
        }

        foreach (Func<bool> p in excludeParts)
        {
            if (p())
                return false;
        }

        bool needOr = includeOrParts.Count > 0;
        bool needAnd = requireAndParts.Count > 0;

        if (!needOr && !needAnd)
            return true;

        bool orOk = !needOr || includeOrParts.Exists(p => p());
        bool andOk = !needAnd || requireAndParts.TrueForAll(p => p());

        return orOk && andOk;
    }

    public void ResetToDefaults()
    {
        if (NoneToggle != null)
            NoneToggle.RowState = CardLibraryFilterTriState.Neutral;
        foreach ((_, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in FlagToggles)
            gui.RowState = CardLibraryFilterTriState.Neutral;
    }
}
