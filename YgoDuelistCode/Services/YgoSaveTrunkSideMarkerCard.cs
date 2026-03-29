using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Save-file trailer only: never offered or played. Marks appended trunk/side blocks in <see cref="SerializablePlayer.Deck"/>.
/// Counts are stored on <see cref="SerializableCard.CurrentUpgradeLevel"/> (trunk) and <see cref="SerializableCard.FloorAddedToDeck"/> (side)
/// so combat replay <see cref="SerializableCard.Serialize"/> never writes custom <see cref="SavedProperties"/> names (those require <see cref="SavedPropertiesTypeCache"/> net IDs).
/// Legacy saves may still use int props <see cref="TrunkCountProp"/> / <see cref="SideCountProp"/>.
/// </summary>
public sealed class YgoSaveTrunkSideMarkerCard : CustomCardModel
{
    public const string TrunkCountProp = "ygo_trunk_count";
    public const string SideCountProp = "ygo_side_count";

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

    public static bool TryReadCounts(SerializableCard marker, out int trunkCount, out int sideCount)
    {
        trunkCount = 0;
        sideCount = 0;
        bool legacy = false;
        if (marker.Props?.ints != null)
        {
            foreach (SavedProperties.SavedProperty<int> p in marker.Props.ints)
            {
                if (p.name == TrunkCountProp)
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

        if (legacy)
            return true;

        trunkCount = marker.CurrentUpgradeLevel;
        sideCount = marker.FloorAddedToDeck ?? 0;
        return true;
    }
}
