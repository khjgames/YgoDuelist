using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Min/max text filter for <see cref="YgoDuelistCard.AdjustedPackWeightMultiplier"/> on the compendium sidebar.</summary>
public sealed class YgoCardLibraryPackWeightRangeFilterState
{
    public NCardViewSortButton? SortButton { get; set; }

    public LineEdit? MinEdit { get; set; }

    public LineEdit? MaxEdit { get; set; }

    public bool Matches(CardModel card)
    {
        float? minBound = ParseOptionalFloat(MinEdit?.Text);
        float? maxBound = ParseOptionalFloat(MaxEdit?.Text);
        if (!minBound.HasValue && !maxBound.HasValue)
            return true;

        float w = card is YgoDuelistCard y ? y.AdjustedPackWeightMultiplier : 1f;
        if (minBound.HasValue && w < minBound.Value)
            return false;
        if (maxBound.HasValue && w > maxBound.Value)
            return false;
        return true;
    }

    static float? ParseOptionalFloat(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
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
