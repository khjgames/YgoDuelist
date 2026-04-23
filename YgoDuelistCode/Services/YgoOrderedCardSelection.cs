using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared helper for synced combat-grid selections whose canonical candidate list is rebuilt by a local callback.
/// Callers own only the candidate builder and gameplay meaning of the chosen cards.
/// This is the preferred helper when card/service code was otherwise repeating a rebuild lambda and typed extraction.
/// </summary>
public static class YgoOrderedCardSelection
{
    public static List<TCard> BuildSingletonCandidates<TCard>(TCard card)
        where TCard : CardModel =>
        [card];

    public static async Task<TCard?> TryChooseSingleAsync<TCard>(
        PlayerChoiceContext choiceContext,
        Player player,
        CardSelectorPrefs prefs,
        Func<List<TCard>> buildCandidates)
        where TCard : CardModel
    {
        List<TCard> candidates = buildCandidates();
        if (candidates.Count == 0)
            return null;

        return await YgoCardGridChoice.TryChooseSingleAsync<TCard>(
            choiceContext,
            candidates.Cast<CardModel>().ToList(),
            player,
            prefs,
            rebuildCanonicalForRemoteApply: () => buildCandidates().Cast<CardModel>().ToList());
    }

    public static async Task<TCard?> TryConfirmSingleCardAsync<TCard>(
        PlayerChoiceContext choiceContext,
        Player player,
        CardSelectorPrefs prefs,
        TCard card)
        where TCard : CardModel =>
        await TryChooseSingleAsync(
            choiceContext,
            player,
            prefs,
            () => BuildSingletonCandidates(card));

    public static async Task<List<TCard>> TryChooseManyAsync<TCard>(
        PlayerChoiceContext choiceContext,
        Player player,
        CardSelectorPrefs prefs,
        Func<List<TCard>> buildCandidates,
        int? maxResults = null,
        PlayerChoiceOptions choiceBegunOptions = PlayerChoiceOptions.None)
        where TCard : CardModel
    {
        List<TCard> candidates = buildCandidates();
        if (candidates.Count == 0)
            return [];

        try
        {
            IEnumerable<CardModel> picked = await TributeSummonGridSelect.FromSimpleGridCombat(
                choiceContext,
                candidates.Cast<CardModel>().ToList(),
                player,
                prefs,
                rebuildCanonicalForRemoteApply: () => buildCandidates().Cast<CardModel>().ToList(),
                choiceBegunOptions);
            IEnumerable<TCard> typed = YgoMpCombatOrder.CardsSnapshotOrderedForMp(picked).OfType<TCard>().Distinct();
            if (maxResults is int max)
                typed = typed.Take(max);
            return typed.ToList();
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }
}
