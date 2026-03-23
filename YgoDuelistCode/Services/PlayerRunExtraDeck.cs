using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Run/map Extra Deck: fusion monsters live here instead of <see cref="Player.Deck"/>.
/// Persisted by merging into save <c>deck</c> entries in <see cref="Patches.PlayerToSerializableAppendYgoExtraDeckPatch"/>.
/// </summary>
public static class PlayerRunExtraDeck
{
    private static readonly ConditionalWeakTable<Player, CardPile> Piles = new();

    public static bool IsYgoDuelistPlayer(Player? player)
    {
        return player?.Character?.Id.Entry == YgoChar.CharacterId;
    }

    public static CardPile GetOrCreatePile(Player player)
    {
        return Piles.GetValue(player, static _ => new CardPile(PileType.None));
    }

    public static CardPile? GetPileIfExists(Player player)
    {
        return Piles.TryGetValue(player, out CardPile? pile) ? pile : null;
    }
}
