using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared accessors for YGO run-state piles that live outside the normal combat pile set.
/// Keeps trunk / side / extra-deck ownership resolution centralized for non-combat systems.
/// </summary>
public static class YgoPlayerRunPiles
{
    public static bool IsYgoRunPlayer(Player? player) =>
        player != null && PlayerRunExtraDeck.IsYgoDuelistPlayer(player);

    public static CardPile? RunExtraDeck(Player? player) =>
        IsYgoRunPlayer(player) ? PlayerRunExtraDeck.GetOrCreatePile(player!) : null;

    public static CardPile? RunExtraDeckIfExists(Player? player) =>
        IsYgoRunPlayer(player) ? PlayerRunExtraDeck.GetPileIfExists(player!) : null;

    public static CardPile? Trunk(Player? player) =>
        IsYgoRunPlayer(player) ? PlayerRunTrunk.GetOrCreatePile(player!) : null;

    public static CardPile? SideDeck(Player? player) =>
        IsYgoRunPlayer(player) ? PlayerRunSideDeck.GetOrCreatePile(player!) : null;

    public static IReadOnlyList<CardModel> RunExtraDeckCards(Player? player) =>
        RunExtraDeck(player)?.Cards.ToList() ?? [];

    public static IReadOnlyList<CardModel> TrunkCards(Player? player) =>
        Trunk(player)?.Cards.ToList() ?? [];

    public static IReadOnlyList<CardModel> SideDeckCards(Player? player) =>
        SideDeck(player)?.Cards.ToList() ?? [];
}
