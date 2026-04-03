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

    public List<(DuelMonsterAttribute Attribute, CardLibraryFilterSortingRuleCategoryFilterToggleGUI Gui)> AttributeToggles { get; } = new();

    /// <summary>OR across checked rows. Cards without a duel attribute context (spells, traps, vanilla, etc.) are not affected.</summary>
    public bool Matches(CardModel card)
    {
        DuelMonsterAttribute? attr = TryGetDuelMonsterAttribute(card);
        if (attr == null)
            return true;

        var includeParts = new List<Func<DuelMonsterAttribute, bool>>();
        var excludeParts = new List<Func<DuelMonsterAttribute, bool>>();

        foreach ((DuelMonsterAttribute a, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in AttributeToggles)
        {
            switch (gui.RowState)
            {
                case CardLibraryFilterTriState.Include:
                {
                    DuelMonsterAttribute captured = a;
                    includeParts.Add(x => x == captured);
                    break;
                }
                case CardLibraryFilterTriState.Exclude:
                {
                    DuelMonsterAttribute captured = a;
                    excludeParts.Add(x => x == captured);
                    break;
                }
            }
        }

        DuelMonsterAttribute v = attr.Value;
        foreach (Func<DuelMonsterAttribute, bool> p in excludeParts)
        {
            if (p(v))
                return false;
        }

        if (includeParts.Count == 0)
            return true;

        foreach (Func<DuelMonsterAttribute, bool> p in includeParts)
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
        foreach ((_, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in AttributeToggles)
            gui.RowState = CardLibraryFilterTriState.Neutral;
    }
}
