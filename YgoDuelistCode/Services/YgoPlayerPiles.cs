using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared accessors for player-owned YGO and vanilla piles.
/// Keeps pile lookup spelling centralized so card/service logic stays focused on rules.
/// </summary>
public static class YgoPlayerPiles
{
    public static CardPile? Hand(Player? player) =>
        player == null ? null : PileType.Hand.GetPile(player);

    public static CardPile? Draw(Player? player) =>
        player == null ? null : PileType.Draw.GetPile(player);

    public static CardPile? Discard(Player? player) =>
        player == null ? null : PileType.Discard.GetPile(player);

    public static CardPile? Graveyard(Player? player) =>
        player == null ? null : GraveyardPile.CustomType.GetPile(player);

    public static IReadOnlyList<CardModel> GraveyardCards(Player? player) =>
        Graveyard(player)?.Cards ?? [];

    public static bool GraveyardContains(Player? player, CardModel card) =>
        Graveyard(player)?.Cards.Contains(card) == true;

    public static CardPile? SpellTrapZone(Player? player) =>
        player == null ? null : SpellTrapZonePile.CustomType.GetPile(player);

    public static CardPile? ExtraDeck(Player? player) =>
        player == null ? null : ExtraDeckPile.CustomType.GetPile(player);

    public static CardPile? Banished(Player? player) =>
        player == null ? null : BanishedPile.CustomType.GetPile(player);

    public static CardPile? Limbo(Player? player) =>
        player == null ? null : LimboPile.CustomType.GetPile(player);

    public static CardPile? MonsterZone(Player? player) =>
        player == null ? null : MonsterPile.CustomType.GetPile(player);

    public static CardPile? Field(Player? player) =>
        player == null ? null : FieldPile.CustomType.GetPile(player);

    public static CardPile? OptionPile(Player? player) =>
        player == null ? null : YgoCardOptionPile.CustomType.GetPile(player);

    public static bool HasOtherHandCard(Player? player, CardModel self) =>
        Hand(player)?.Cards.Any(c => !ReferenceEquals(c, self)) == true;

    public static List<CardModel> OrderedCardsFromPiles(
        Player? player,
        params Func<Player?, CardPile?>[] pileGetters)
    {
        var cards = new List<CardModel>();
        if (player == null)
            return cards;

        foreach (Func<Player?, CardPile?> getPile in pileGetters)
        {
            CardPile? pile = getPile(player);
            if (pile == null)
                continue;

            cards.AddRange(YgoMpCombatOrder.CardsSnapshotOrderedForMp(pile.Cards));
        }

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(cards);
    }

    public static List<TCard> OrderedCardsOfTypeFromPiles<TCard>(
        Player? player,
        params Func<Player?, CardPile?>[] pileGetters)
        where TCard : CardModel =>
        OrderedCardsFromPiles(player, pileGetters).OfType<TCard>().ToList();
}
