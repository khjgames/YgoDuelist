using MegaCrit.Sts2.Core.Entities.Cards;

namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// Piles where right-click cycle hint keywords (20039 / 20040) should appear on card UI.
/// Not used in field zones, extra deck, spell/trap zone, option piles, etc.
/// </summary>
internal static class YgoCycleKeywordPileHelper
{
    public static bool AllowsCycleKeywordHints(PileType pileType) =>
        pileType is PileType.Hand
            or PileType.Deck
            or PileType.Draw
            or PileType.Exhaust
            or PileType.Discard;
}
