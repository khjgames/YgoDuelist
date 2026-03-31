using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Dummy properties so <see cref="SavedPropertiesTypeCache"/> maps the same strings used on the deck trailer
/// <see cref="YgoSaveTrunkSideMarkerCard"/> (<c>SerializableCard.Props.ints</c>). Required for
/// <see cref="SavedProperties.Serialize"/> / multiplayer net IDs.
/// </summary>
public sealed class YgoSaveTrunkSideMarkerNetPropertyNames
{
    [SavedProperty]
    public int ygo_extra_count { get; set; }

    [SavedProperty]
    public int ygo_min_deck_size { get; set; }

    [SavedProperty]
    public int ygo_owed_rare_vouchers { get; set; }
}
