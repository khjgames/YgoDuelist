using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Compendium duel monster level (stars) filter for one <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary"/> instance.</summary>
public sealed class YgoCardLibraryLevelFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    /// <summary>Index 0 = level 1 … index 11 = level 12.</summary>
    public CardLibraryFilterSortingRuleCategoryFilterToggleGUI?[] LevelToggles { get; } = new CardLibraryFilterSortingRuleCategoryFilterToggleGUI?[12];

    /// <summary>OR across checked rows. Cards without a duel level (spells, traps, vanilla, etc.) are not affected.</summary>
    public bool Matches(CardModel card)
    {
        int? lv = TryGetDuelMonsterLevel(card);
        if (lv == null)
            return true;

        var parts = new List<Func<int, bool>>();

        for (int i = 0; i < 12; i++)
        {
            if (LevelToggles[i]?.IsTicked == true)
            {
                int levelValue = i + 1;
                parts.Add(l => l == levelValue);
            }
        }

        if (parts.Count == 0)
            return true;

        int v = lv.Value;
        foreach (Func<int, bool> p in parts)
        {
            if (p(v))
                return true;
        }

        return false;
    }

    static int? TryGetDuelMonsterLevel(CardModel card)
    {
        if (card is BaseMonsterCard bm)
            return bm.IsCanonical ? bm.DuelMonsterLevel : bm.GetEffectiveDuelMonsterLevel();
        if (card is AbstractMonsterCard am)
            return am.DuelMonsterLevel;
        if (card is MonsterCommandCard cmd && cmd.SourceMonster is BaseMonsterCard src)
            return src.IsCanonical ? src.DuelMonsterLevel : src.GetEffectiveDuelMonsterLevel();
        return null;
    }

    public void ResetToDefaults()
    {
        foreach (CardLibraryFilterSortingRuleCategoryFilterToggleGUI? t in LevelToggles)
        {
            if (t != null)
                t.IsTicked = false;
        }
    }
}
