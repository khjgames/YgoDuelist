using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Compendium duel race / type line filter for one <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary"/> instance.</summary>
public sealed class YgoCardLibraryRaceFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI? AnyToggle { get; set; }

    public List<(DuelMonsterRace Race, CardLibraryFilterSortingRuleCategoryFilterToggleGUI Gui)> RaceToggles { get; } = new();

    /// <summary>OR across checked rows. Non–YGO cards and command cards without a source monster are not affected.</summary>
    public bool Matches(CardModel card)
    {
        DuelMonsterRace? race = TryGetDuelMonsterRace(card);
        if (race == null)
            return true;

        var parts = new List<Func<DuelMonsterRace, bool>>();

        if (AnyToggle?.IsTicked == true)
            parts.Add(_ => true);

        foreach ((DuelMonsterRace r, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in RaceToggles)
        {
            if (gui.IsTicked)
            {
                DuelMonsterRace captured = r;
                parts.Add(x => x == captured);
            }
        }

        if (parts.Count == 0)
            return true;

        DuelMonsterRace v = race.Value;
        foreach (Func<DuelMonsterRace, bool> p in parts)
        {
            if (p(v))
                return true;
        }

        return false;
    }

    static DuelMonsterRace? TryGetDuelMonsterRace(CardModel card)
    {
        if (card is MonsterCommandCard cmd)
            return cmd.SourceMonster != null ? cmd.SourceMonster.DuelMonsterRace : null;
        if (card is IYgoCard ygo)
            return ygo.DuelMonsterRace;
        return null;
    }

    public void ResetToDefaults()
    {
        if (AnyToggle != null)
            AnyToggle.IsTicked = true;
        foreach ((_, CardLibraryFilterSortingRuleCategoryFilterToggleGUI gui) in RaceToggles)
            gui.IsTicked = false;
    }
}
