using System;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Character;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Save-file trailer only: never offered or played. Marks appended extra/trunk/side blocks in <see cref="SerializablePlayer.Deck"/>.
/// Trunk/side counts use <see cref="SerializableCard.CurrentUpgradeLevel"/> / <see cref="SerializableCard.FloorAddedToDeck"/>.
/// Extra deck, min deck, owed rare vouchers, and pack tag balance use <see cref="SavedProperties"/> keys on the
/// marker's <see cref="SerializableCard.Props"/>; the matching <see cref="SavedProperty"/> fields below register net IDs
/// via <see cref="SavedPropertiesTypeCache"/> for replay / multiplayer serialization.
/// Minimum deck size and <see cref="OwedRareCardVouchersProp"/> use int props when non-default.
/// </summary>
[Pool(typeof(YgoDuelistCardPool))]
public sealed class YgoSaveTrunkSideMarkerCard : CustomCardModel
{
    public const string ExtraDeckCountProp = "YgoDuelist_ygo_extra_count";
    public const string MinDeckSizeProp = "ygo_min_deck_size";
    public const string MinDeckReceivedCardProgressProp = "ygo_min_deck_received_card_progress";
    public const string OwedRareCardVouchersProp = "ygo_owed_rare_vouchers";
    public const string PackTagBalanceProp = "ygo_pack_tag_balance";

    /// <summary>Registers <see cref="ExtraDeckCountProp"/> with <see cref="SavedPropertiesTypeCache"/> (trailer uses Props.ints).</summary>
    [SavedProperty]
    public int YgoDuelist_ygo_extra_count { get; set; }

    /// <summary>Registers <see cref="MinDeckSizeProp"/> with <see cref="SavedPropertiesTypeCache"/>.</summary>
    [SavedProperty]
    public int ygo_min_deck_size { get; set; }

    /// <summary>Registers <see cref="MinDeckReceivedCardProgressProp"/> with <see cref="SavedPropertiesTypeCache"/>.</summary>
    [SavedProperty]
    public int ygo_min_deck_received_card_progress { get; set; }

    /// <summary>Registers <see cref="OwedRareCardVouchersProp"/> with <see cref="SavedPropertiesTypeCache"/>.</summary>
    [SavedProperty]
    public int ygo_owed_rare_vouchers { get; set; }

    /// <summary>Registers <see cref="PackTagBalanceProp"/> with <see cref="SavedPropertiesTypeCache"/>.</summary>
    [SavedProperty]
    public string ygo_pack_tag_balance { get; set; } = "";

    public const int MaxSerializedPileCount = 255;

    /// <summary>Clamp for <see cref="OwedRareCardVouchersProp"/> (run state; pack rare IOU).</summary>
    public const int MaxSerializedOwedRareVouchers = 255;

    /// <summary>
    /// Trunk count is stored in <see cref="SerializableCard.CurrentUpgradeLevel"/> (see
    /// <c>PlayerToSerializableAppendYgoTrunkSidePatch</c>). Must allow the full serialized range or
    /// <see cref="CardModel.FromSerializable"/> throws on multiplayer sync / load.
    /// </summary>
    public override int MaxUpgradeLevel => MaxSerializedPileCount;

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

    public static int ReadMinimumDeckReceivedCardProgressOrDefault(SerializableCard marker)
    {
        if (marker.Props?.ints == null)
            return 0;

        foreach (SavedProperties.SavedProperty<int> p in marker.Props.ints)
        {
            if (p.name == MinDeckReceivedCardProgressProp)
                return Math.Clamp(p.value, 0, YgoPlayerMinimumDeck.ReceivedCardsPerMinimumIncrease - 1);
        }

        return 0;
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

    public static string? ReadPackTagBalanceOrNull(SerializableCard marker)
    {
        if (marker.Props?.strings == null)
            return null;

        foreach (SavedProperties.SavedProperty<string> p in marker.Props.strings)
        {
            if (p.name == PackTagBalanceProp)
                return p.value;
        }

        return null;
    }

    public static bool TryReadCounts(SerializableCard marker, out int trunkCount, out int sideCount)
    {
        ReadTrailerCounts(marker, out _, out trunkCount, out sideCount);
        return true;
    }
}
