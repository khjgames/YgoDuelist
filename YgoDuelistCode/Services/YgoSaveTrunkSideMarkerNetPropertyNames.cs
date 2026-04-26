using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Dummy properties so <see cref="SavedPropertiesTypeCache"/> maps the same names used on the deck trailer
/// <see cref="YgoSaveTrunkSideMarkerCard"/> (<c>SerializableCard.Props.ints</c> / <c>.strings</c>). Required for
/// <see cref="SavedProperties.Serialize"/> / combat replay net IDs.
/// </summary>
public sealed class YgoSaveTrunkSideMarkerNetPropertyNames
{
    [SavedProperty]
    public int ygo_extra_count { get; set; }

    [SavedProperty]
    public int ygo_min_deck_size { get; set; }

    [SavedProperty]
    public int ygo_min_deck_received_card_progress { get; set; }

    [SavedProperty]
    public int ygo_owed_rare_vouchers { get; set; }

    [SavedProperty]
    public string ygo_pack_tag_balance { get; set; } = "";
}
