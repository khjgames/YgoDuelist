using System;
using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Min/max text filter for one monster stat (ATK or DEF) on the compendium sidebar.</summary>
public sealed class YgoCardLibraryStatRangeFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    public LineEdit? MinEdit { get; set; }

    public LineEdit? MaxEdit { get; set; }

    /// <summary>
    /// When the card has no printed stat (spells, traps, vanilla, non-<see cref="BaseMonsterCard"/>), this section does not exclude it.
    /// </summary>
    public bool Matches(CardModel card, Func<BaseMonsterCard, int> getStat)
    {
        if (card is not BaseMonsterCard bm)
            return true;

        int? minBound = ParseOptionalInt(MinEdit?.Text);
        int? maxBound = ParseOptionalInt(MaxEdit?.Text);
        if (!minBound.HasValue && !maxBound.HasValue)
            return true;

        int stat = getStat(bm);
        if (minBound.HasValue && stat < minBound.Value)
            return false;
        if (maxBound.HasValue && stat > maxBound.Value)
            return false;
        return true;
    }

    static int? ParseOptionalInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            return v;
        return null;
    }

    public void ResetToDefaults()
    {
        if (MinEdit != null)
            MinEdit.Text = string.Empty;
        if (MaxEdit != null)
            MaxEdit.Text = string.Empty;
    }
}
