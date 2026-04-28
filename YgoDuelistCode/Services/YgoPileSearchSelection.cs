using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public enum YgoSearchPile
{
    Hand,
    Draw,
    Discard,
    Graveyard,
    Banished,
    ExtraDeck,
    SpellTrapZone,
    MonsterZone,
    Field,
    OptionPile
}

public static class YgoPileSearchSelection
{
    public static List<CardModel> BuildCandidates(
        Player? player,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        Func<CardModel, bool> predicate)
    {
        if (player == null)
            return [];

        var cards = new List<CardModel>();

        foreach (YgoSearchPile sourcePile in sourcePiles)
        {
            CardPile? pile = GetPile(player, sourcePile);
            if (pile == null)
                continue;

            cards.AddRange(YgoMpCombatOrder.CardsSnapshotOrderedForMp(pile.Cards).Where(predicate));
        }

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(cards).ToList();
    }

    public static List<TCard> BuildCandidates<TCard>(
        Player? player,
        IReadOnlyList<YgoSearchPile> sourcePiles)
        where TCard : CardModel =>
        BuildCandidates(player, sourcePiles, c => c is TCard)
            .OfType<TCard>()
            .ToList();

    public static async Task<List<CardModel>> TrySelectAsync(
        Player player,
        LocString prompt,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        Func<CardModel, bool> predicate,
        int minSelect,
        int maxSelect,
        bool cancelable = true,
        bool requireManualConfirmation = true)
    {
        var prefs = new CardSelectorPrefs(prompt, minSelect, maxSelect)
        {
            Cancelable = cancelable,
            RequireManualConfirmation = requireManualConfirmation
        };

        return await YgoOrderedCardSelection.TryChooseManyAsync(
            YgoChoiceContexts.Blocking(),
            player,
            prefs,
            () => BuildCandidates(player, sourcePiles, predicate),
            maxResults: maxSelect);
    }

    public static async Task<List<TCard>> TrySelectAsync<TCard>(
        Player player,
        LocString prompt,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        int minSelect,
        int maxSelect,
        bool cancelable = true,
        bool requireManualConfirmation = true)
        where TCard : CardModel
    {
        var prefs = new CardSelectorPrefs(prompt, minSelect, maxSelect)
        {
            Cancelable = cancelable,
            RequireManualConfirmation = requireManualConfirmation
        };

        return await YgoOrderedCardSelection.TryChooseManyAsync(
            YgoChoiceContexts.Blocking(),
            player,
            prefs,
            () => BuildCandidates<TCard>(player, sourcePiles),
            maxResults: maxSelect);
    }

    public static async Task<int> TrySearchToPileAsync(
        Player player,
        LocString prompt,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        YgoSearchPile destinationPile,
        Func<CardModel, bool> predicate,
        int minSelect,
        int maxSelect,
        bool cancelable = true,
        bool requireManualConfirmation = true,
        CardPilePosition position = CardPilePosition.Top,
        bool skipVisuals = false)
    {
        List<CardModel> chosen = await TrySelectAsync(
            player,
            prompt,
            sourcePiles,
            predicate,
            minSelect,
            maxSelect,
            cancelable,
            requireManualConfirmation);

        return await MoveToPileAsync(player, chosen, destinationPile, position, skipVisuals);
    }

    public static async Task<int> TrySearchToPileAsync<TCard>(
        Player player,
        LocString prompt,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        YgoSearchPile destinationPile,
        int minSelect,
        int maxSelect,
        bool cancelable = true,
        bool requireManualConfirmation = true,
        CardPilePosition position = CardPilePosition.Top,
        bool skipVisuals = false)
        where TCard : CardModel
    {
        List<TCard> chosen = await TrySelectAsync<TCard>(
            player,
            prompt,
            sourcePiles,
            minSelect,
            maxSelect,
            cancelable,
            requireManualConfirmation);

        return await MoveToPileAsync(
            player,
            chosen.Cast<CardModel>().ToList(),
            destinationPile,
            position,
            skipVisuals);
    }

    public static async Task<int> TrySearchToHandAsync(
        Player player,
        LocString prompt,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        Func<CardModel, bool> predicate,
        int minSelect,
        int maxSelect,
        bool cancelable = true,
        bool requireManualConfirmation = true) =>
        await TrySearchToPileAsync(
            player,
            prompt,
            sourcePiles,
            YgoSearchPile.Hand,
            predicate,
            minSelect,
            maxSelect,
            cancelable,
            requireManualConfirmation);

    public static async Task<int> TrySearchToHandAsync<TCard>(
        Player player,
        LocString prompt,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        int minSelect,
        int maxSelect,
        bool cancelable = true,
        bool requireManualConfirmation = true)
        where TCard : CardModel =>
        await TrySearchToPileAsync<TCard>(
            player,
            prompt,
            sourcePiles,
            YgoSearchPile.Hand,
            minSelect,
            maxSelect,
            cancelable,
            requireManualConfirmation);

    private static async Task<int> MoveToPileAsync(
        Player player,
        IReadOnlyList<CardModel> cards,
        YgoSearchPile destinationPile,
        CardPilePosition position,
        bool skipVisuals)
    {
        if (cards.Count == 0)
            return 0;

        CardPile? destination = GetPile(player, destinationPile);
        if (destination == null)
            return 0;

        int moved = 0;
        foreach (CardModel card in YgoMpCombatOrder.CardsSnapshotOrderedForMp(cards))
        {
            await CardPileCmd.Add(
                new CardModel[] { card },
                destination,
                position,
                card,
                skipVisuals);

            moved++;
        }

        return moved;
    }

    private static CardPile? GetPile(Player player, YgoSearchPile sourcePile) =>
        sourcePile switch
        {
            YgoSearchPile.Hand => YgoPlayerPiles.Hand(player),
            YgoSearchPile.Draw => YgoPlayerPiles.Draw(player),
            YgoSearchPile.Discard => YgoPlayerPiles.Discard(player),
            YgoSearchPile.Graveyard => YgoPlayerPiles.Graveyard(player),
            YgoSearchPile.Banished => YgoPlayerPiles.Banished(player),
            YgoSearchPile.ExtraDeck => YgoPlayerPiles.ExtraDeck(player),
            YgoSearchPile.SpellTrapZone => YgoPlayerPiles.SpellTrapZone(player),
            YgoSearchPile.MonsterZone => YgoPlayerPiles.MonsterZone(player),
            YgoSearchPile.Field => YgoPlayerPiles.Field(player),
            YgoSearchPile.OptionPile => YgoPlayerPiles.OptionPile(player),
            _ => null
        };
}