using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Run/map Extra Deck: fusion monsters live here instead of <see cref="Player.Deck"/>.
/// Persisted in save <c>deck</c> via <see cref="Patches.PlayerToSerializableAppendYgoExtraDeckPatch"/> and stripped on load by <see cref="Patches.PlayerLoadInventoryStripYgoTrunkSidePatch"/>.
/// </summary>
public static class PlayerRunExtraDeck
{
    private static readonly ConditionalWeakTable<Player, CardPile> Piles = new();

    /// <summary>True when this player is the YgoDuelist character (by runtime type, not <c>Id.Entry</c> — BaseLib uses e.g. YGODUELIST-YGO_DUELIST).</summary>
    public static bool IsYgoDuelistPlayer(Player? player)
    {
        return player?.Character is YgoChar;
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
