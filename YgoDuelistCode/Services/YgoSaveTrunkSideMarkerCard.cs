using System;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Save-file trailer only: never offered or played. Marks appended extra/trunk/side blocks in <see cref="SerializablePlayer.Deck"/>.
/// Trunk/side counts use <see cref="SerializableCard.CurrentUpgradeLevel"/> / <see cref="SerializableCard.FloorAddedToDeck"/> (no custom prop names in combat replay).
/// Extra deck count uses <see cref="ExtraDeckCountProp"/> on <see cref="SavedProperties"/> (save trailer only).
/// Minimum deck size and <see cref="OwedRareCardVouchersProp"/> use int props when non-default.
/// </summary>
public sealed class YgoSaveTrunkSideMarkerCard : CustomCardModel
{
    public const string ExtraDeckCountProp = "ygo_extra_count";
    public const string MinDeckSizeProp = "ygo_min_deck_size";
    public const string OwedRareCardVouchersProp = "ygo_owed_rare_vouchers";

    public const int MaxSerializedPileCount = 255;

    /// <summary>Clamp for <see cref="OwedRareCardVouchersProp"/> (run state; pack rare IOU).</summary>
    public const int MaxSerializedOwedRareVouchers = 255;

    public YgoSaveTrunkSideMarkerCard()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false, autoAdd: false)
    {
    }

    public static bool IsMarker(SerializableCard? card, ModelId? markerId)
    {
        if (card?.Id == null || markerId == null)
            return false;
        return card.Id.Equals(markerId);
    }

    public static void ReadTrailerCounts(SerializableCard marker, out int extraDeckCount, out int trunkCount, out int sideCount)
    {
        extraDeckCount = 0;
        trunkCount = marker.CurrentUpgradeLevel;
        sideCount = marker.FloorAddedToDeck ?? 0;
        if (marker.Props?.ints != null)
        {
            foreach (SavedProperties.SavedProperty<int> p in marker.Props.ints)
            {
                if (p.name == ExtraDeckCountProp)
                    extraDeckCount = Math.Clamp(p.value, 0, MaxSerializedPileCount);
            }
        }

        trunkCount = Math.Clamp(trunkCount, 0, MaxSerializedPileCount);
        sideCount = Math.Clamp(sideCount, 0, MaxSerializedPileCount);
    }

    public static int ReadMinimumDeckSizeOrDefault(SerializableCard marker, int defaultMinimum)
    {
        if (marker.Props?.ints == null)
            return defaultMinimum;

        foreach (SavedProperties.SavedProperty<int> p in marker.Props.ints)
        {
            if (p.name == MinDeckSizeProp)
                return Math.Max(defaultMinimum, p.value);
        }

        return defaultMinimum;
    }

    public static int ReadOwedRareCardVouchersOrDefault(SerializableCard marker)
    {
        if (marker.Props?.ints == null)
            return 0;

        foreach (SavedProperties.SavedProperty<int> p in marker.Props.ints)
        {
            if (p.name == OwedRareCardVouchersProp)
                return Math.Clamp(p.value, 0, MaxSerializedOwedRareVouchers);
        }

        return 0;
    }

    public static bool TryReadCounts(SerializableCard marker, out int trunkCount, out int sideCount)
    {
        ReadTrailerCounts(marker, out _, out trunkCount, out sideCount);
        return true;
    }
}
