using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Save-file trailer only: never offered or played. Marks appended trunk/side blocks in <see cref="SerializablePlayer.Deck"/>.
/// </summary>
public sealed class YgoSaveTrunkSideMarkerCard : CustomCardModel
{
    public const string TrunkCountProp = "ygo_trunk_count";
    public const string SideCountProp = "ygo_side_count";

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
        if (marker.Props?.ints == null)
            return false;

        foreach (SavedProperties.SavedProperty<int> p in marker.Props.ints)
        {
            if (p.name == TrunkCountProp)
                trunkCount = p.value;
            else if (p.name == SideCountProp)
                sideCount = p.value;
        }

        return true;
    }
}
