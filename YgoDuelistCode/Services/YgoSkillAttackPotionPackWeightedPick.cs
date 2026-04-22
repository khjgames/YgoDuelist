using System.Collections.Generic;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Weighted draws for YGO skill/attack potions using each card's <see cref="YgoDuelistCard.PackWeightMultiplier"/>
/// (same notion as pack generation in <see cref="YgoCardPackGenerator"/>).
/// </summary>
public static class YgoSkillAttackPotionPackWeightedPick
{
    /// <summary>Non-positive weights would break <see cref="GrabBag{T}"/>; clamp to a tiny positive value.</summary>
    public static double GetSelectionWeight(CardModel c) =>
        c is YgoDuelistCard y ? System.Math.Max((double)y.PackWeightMultiplier, 1e-9) : 1.0;

    /// <summary>
    /// Up to <paramref name="count"/> distinct picks without replacement. Pool must already be in deterministic order
    /// (e.g. sorted by <see cref="CardId.Entry"/>) so multiplayer peers agree on tie-breaking.
    /// </summary>
    public static List<CardModel> TakeDistinctWeighted(IReadOnlyList<CardModel> sortedPool, int count, Rng rng)
    {
        int take = System.Math.Min(count, sortedPool.Count);
        var remaining = new List<CardModel>(sortedPool);
        var picks = new List<CardModel>(take);
        for (int p = 0; p < take; p++)
        {
            var bag = new GrabBag<CardModel>();
            foreach (CardModel c in remaining)
                bag.Add(c, GetSelectionWeight(c));
            CardModel? choice = bag.GrabAndRemove(rng);
            if (choice == null)
                break;
            picks.Add(choice);
            remaining.Remove(choice);
        }

        return picks;
    }
}
