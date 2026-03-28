using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Run/map Side Deck; persisted via <see cref="Patches.PlayerToSerializableAppendYgoTrunkSidePatch"/>.
/// </summary>
public static class PlayerRunSideDeck
{
    private static readonly ConditionalWeakTable<Player, CardPile> Piles = new();

    public static CardPile GetOrCreatePile(Player player)
    {
        return Piles.GetValue(player, static _ => new CardPile(PileType.None));
    }

    public static CardPile? GetPileIfExists(Player player)
    {
        return Piles.TryGetValue(player, out CardPile? pile) ? pile : null;
    }
}
