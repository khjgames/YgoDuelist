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
/// Legacy saves may still use int props <see cref="TrunkCountProp"/> / <see cref="SideCountProp"/> for trunk/side.
/// </summary>
public sealed class YgoSaveTrunkSideMarkerCard : CustomCardModel
{
    public const string TrunkCountProp = "ygo_trunk_count";
    public const string SideCountProp = "ygo_side_count";
    public const string ExtraDeckCountProp = "ygo_extra_count";
    public const string MinDeckSizeProp = "ygo_min_deck_size";

    public const int MaxSerializedPileCount = 255;

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
        trunkCount = 0;
        sideCount = 0;
        bool legacy = false;
        if (marker.Props?.ints != null)
        {
            foreach (SavedProperties.SavedProperty<int> p in marker.Props.ints)
            {
                if (p.name == ExtraDeckCountProp)
                    extraDeckCount = Math.Clamp(p.value, 0, MaxSerializedPileCount);
                else if (p.name == TrunkCountProp)
                {
                    trunkCount = p.value;
                    legacy = true;
                }
                else if (p.name == SideCountProp)
                {
                    sideCount = p.value;
                    legacy = true;
                }
            }
        }

        if (!legacy)
        {
            trunkCount = marker.CurrentUpgradeLevel;
            sideCount = marker.FloorAddedToDeck ?? 0;
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

    public static bool TryReadCounts(SerializableCard marker, out int trunkCount, out int sideCount)
    {
        ReadTrailerCounts(marker, out _, out trunkCount, out sideCount);
        return true;
    }
}
