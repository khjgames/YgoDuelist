using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Compendium duel monster attribute filter for one <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary"/> instance.</summary>
public sealed class YgoCardLibraryAttributeFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? AnyToggle { get; set; }

    public List<(DuelMonsterAttribute Attribute, CardLibraryFilterSortingRuleCategoryFilterToggleGUI Gui)> AttributeToggles { get; } = new();

    /// <summary>OR across checked rows. Cards without a duel attribute context (spells, traps, vanilla, etc.) are not affected.</summary>
    public bool Matches(CardModel card)
    {
        DuelMonsterAttribute? attr = TryGetDuelMonsterAttribute(card);
        if (attr == null)
            return true;

        var parts = new List<Func<DuelMonsterAttribute, bool>>();

        if (AnyToggle?.IsTicked == true)
            parts.Add(_ => true);

        foreach ((DuelMonsterAttribute a, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in AttributeToggles)
        {
            if (gui.IsTicked)
            {
                DuelMonsterAttribute captured = a;
                parts.Add(x => x == captured);
            }
        }

        if (parts.Count == 0)
            return true;

        DuelMonsterAttribute v = attr.Value;
        foreach (Func<DuelMonsterAttribute, bool> p in parts)
        {
            if (p(v))
                return true;
        }

        return false;
    }

    static DuelMonsterAttribute? TryGetDuelMonsterAttribute(CardModel card)
    {
        if (card is AbstractMonsterCard am)
            return am.DuelMonsterAttribute;
        if (card is MonsterCommandCard cmd && cmd.SourceMonster != null)
            return cmd.SourceMonster.DuelMonsterAttribute;
        return null;
    }

    public void ResetToDefaults()
    {
        if (AnyToggle != null)
            AnyToggle.IsTicked = true;
        foreach ((_, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in AttributeToggles)
            gui.IsTicked = false;
    }
}
