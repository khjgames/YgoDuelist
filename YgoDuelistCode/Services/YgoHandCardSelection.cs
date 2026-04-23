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
/// Shared helper for synced choose-one hand-card grids that exclude the source card or apply a simple filter.
/// </summary>
public static class YgoHandCardSelection
{
    public static async Task<TCard?> TryChooseSingleHandCardAsync<TCard>(
        PlayerChoiceContext choiceContext,
        Player player,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? predicate = null,
        CardModel? excludeReference = null,
        PlayerChoiceOptions choiceBegunOptions = PlayerChoiceOptions.None)
        where TCard : CardModel
    {
        List<TCard> BuildHandCandidates() =>
            BuildHandCandidates<TCard>(player, predicate, excludeReference);

        List<TCard> candidates = BuildHandCandidates();
        if (candidates.Count == 0)
            return null;

        return await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            prefs,
            BuildHandCandidates);
    }

    private static List<TCard> BuildHandCandidates<TCard>(
        Player player,
        Func<CardModel, bool>? predicate,
        CardModel? excludeReference)
        where TCard : CardModel =>
        TributeSummonGridSelect.BuildStabilizedHandCandidates(player, predicate, excludeReference)
            .OfType<TCard>()
            .ToList();
}
