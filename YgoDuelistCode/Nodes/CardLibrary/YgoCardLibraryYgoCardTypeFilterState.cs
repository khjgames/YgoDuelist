using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Compendium YGO frame-type filter for one <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary"/> instance.</summary>
public sealed class YgoCardLibraryYgoCardTypeFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? AnyToggle { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? NormalMonsterToggle { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? EffectMonsterToggle { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? TrapToggle { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? SpellToggle { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? FusionMonsterToggle { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? RitualMonsterToggle { get; set; }

    /// <summary>OR across checked rows (same idea as pack-tag filters). Non–YGO-framed cards are not affected.</summary>
    public bool Matches(CardModel card)
    {
        if (card is not IYgoCard ygo)
            return true;

        var parts = new List<Func<IYgoCard, bool>>();

        if (AnyToggle?.IsTicked == true)
            parts.Add(_ => true);

        if (NormalMonsterToggle?.IsTicked == true)
            parts.Add(y => y.YgoCardType == YgoCardType.Monster);

        if (EffectMonsterToggle?.IsTicked == true)
            parts.Add(y => y.YgoCardType == YgoCardType.EffectMonster);

        if (TrapToggle?.IsTicked == true)
            parts.Add(y => y.YgoCardType == YgoCardType.Trap);

        if (SpellToggle?.IsTicked == true)
            parts.Add(y => y.YgoCardType == YgoCardType.Spell);

        if (FusionMonsterToggle?.IsTicked == true)
            parts.Add(y => y.YgoCardType == YgoCardType.FusionMonster);

        if (RitualMonsterToggle?.IsTicked == true)
            parts.Add(y => y.YgoCardType == YgoCardType.RitualMonster);

        if (parts.Count == 0)
            return true;

        foreach (Func<IYgoCard, bool> p in parts)
        {
            if (p(ygo))
                return true;
        }

        return false;
    }

    public void ResetToDefaults()
    {
        if (AnyToggle != null)
            AnyToggle.IsTicked = true;
        if (NormalMonsterToggle != null)
            NormalMonsterToggle.IsTicked = false;
        if (EffectMonsterToggle != null)
            EffectMonsterToggle.IsTicked = false;
        if (TrapToggle != null)
            TrapToggle.IsTicked = false;
        if (SpellToggle != null)
            SpellToggle.IsTicked = false;
        if (FusionMonsterToggle != null)
            FusionMonsterToggle.IsTicked = false;
        if (RitualMonsterToggle != null)
            RitualMonsterToggle.IsTicked = false;
    }
}
