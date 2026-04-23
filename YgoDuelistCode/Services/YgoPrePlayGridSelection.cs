using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared helpers for card-owned <c>IYgoPrePlayCancelableGridSelection</c> implementations.
/// Cards still own candidate construction and payload meaning; this helper removes repeated grid boilerplate.
/// </summary>
public static class YgoPrePlayGridSelection
{
    public static async Task<bool> TryPrepareSingleCardPayloadAsync<TCard>(
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> candidates,
        CardSelectorPrefs prefs,
        Func<List<CardModel>>? rebuildCanonicalForRemoteApply = null)
        where TCard : CardModel
    {
        List<TCard> BuildPayloadCandidates() =>
            BuildCanonicalCandidates<TCard>(candidates, rebuildCanonicalForRemoteApply);

        TCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoChoiceContexts.Blocking(),
            player,
            prefs,
            BuildPayloadCandidates);
        if (chosen == null)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    public static async Task<bool> TryPrepareSingleOptionIdPayloadAsync(
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> options,
        CardSelectorPrefs prefs,
        Func<List<CardModel>>? rebuildCanonicalForRemoteApply = null)
    {
        List<YgoTransientSpellOptionCommandCard> BuildOptionCards() =>
            BuildCanonicalCandidates<YgoTransientSpellOptionCommandCard>(options, rebuildCanonicalForRemoteApply);

        YgoTransientSpellOptionCommandCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoChoiceContexts.Blocking(),
            player,
            prefs,
            BuildOptionCards);
        if (chosen == null)
            return false;

        YgoPrePlayOptionIdPayload.SetPending(sourceCard, chosen.OptionId);
        return true;
    }

    private static List<TCard> BuildCanonicalCandidates<TCard>(
        IReadOnlyList<CardModel> candidates,
        Func<List<CardModel>>? rebuildCanonicalForRemoteApply)
        where TCard : CardModel =>
        (rebuildCanonicalForRemoteApply?.Invoke() ?? candidates.ToList())
            .OfType<TCard>()
            .ToList();
}
