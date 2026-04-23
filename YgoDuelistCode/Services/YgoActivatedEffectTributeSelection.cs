using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared helper for monster activated effects that preselect one or more tribute field monsters.
/// </summary>
public static class YgoActivatedEffectTributeSelection
{
    private static List<BaseMonsterCard> BuildTributeCandidates(IReadOnlyList<CardModel> candidates) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(candidates).OfType<BaseMonsterCard>().ToList();

    public static async Task<bool> TryPrepareSingleTributeAsync(
        Player player,
        NormalMonsterCard source,
        IReadOnlyList<CardModel> candidates,
        LocString prompt)
    {
        if (candidates.Count == 0)
            return false;

        var prefs = new CardSelectorPrefs(prompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true,
        };

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoChoiceContexts.Blocking(),
            player,
            prefs,
            () => BuildTributeCandidates(candidates));
        if (chosen == null)
            return false;

        ActivatedEffectTributeSelectionPayload.SetPending(source, chosen);
        return true;
    }

    public static async Task<bool> TryPrepareExactTributesAsync(
        Player player,
        NormalMonsterCard source,
        IReadOnlyList<CardModel> candidates,
        int tributeCount,
        LocString prompt)
    {
        if (tributeCount <= 0 || candidates.Count < tributeCount)
            return false;

        var prefs = new CardSelectorPrefs(prompt, tributeCount, tributeCount)
        {
            RequireManualConfirmation = true,
            Cancelable = true,
        };

        List<BaseMonsterCard> list = await YgoOrderedCardSelection.TryChooseManyAsync(
            YgoChoiceContexts.Blocking(),
            player,
            prefs,
            () => BuildTributeCandidates(candidates),
            tributeCount);
        list = list.Distinct().Take(tributeCount).ToList();
        if (list.Count != tributeCount)
            return false;

        ObeliskActivatedTributePayload.SetPending(source, list);
        return true;
    }
}
