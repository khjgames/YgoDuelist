using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Same-rarity bulk mates for <see cref="YgoDuelistCard.BulkBundled"/> constrained by the exact discovery tag context
/// (pack/shop row mask bit that produced the anchor card).
/// </summary>
public static class YgoBulkBundledResolver
{
    private static readonly object NoBulkMateSentinel = new();

    private static readonly ConditionalWeakTable<MerchantCardEntry, object> MerchantBulkMateByEntry = new();

    /// <summary>
    /// Unlocked YGO templates with the anchor's rarity, <see cref="YgoDuelistCard.BulkBundled"/> enabled, and matching the
    /// effective tag context that discovered the anchor (<paramref name="discoveryTagMask"/>).
    /// </summary>
    public static List<CardModel> GetEligibleBulkTemplates(Player player, YgoDuelistCard anchor, YgoCardPackTags discoveryTagMask)
    {
        YgoCardPackTags anchorTags = YgoPackCardCatalog.GetEffectivePackTags(anchor);
        YgoCardPackTags activeContextTags = anchorTags & discoveryTagMask;
        if (activeContextTags == YgoCardPackTags.None)
            return [];

        HashSet<ModelId> unlocked = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(c => c.Id)
            .ToHashSet();

        CardRarity r = anchor.Rarity;
        ModelId anchorId = anchor.Id;

        return YgoPackCardCatalog.GetAllYgoTemplates()
            .Where(c =>
                c.Rarity == r
                && c.Id != anchorId
                && unlocked.Contains(c.Id)
                && c is YgoDuelistCard y
                && y.BulkBundled
                && (YgoPackCardCatalog.GetEffectivePackTags(y) & activeContextTags) != 0
                && !YgoPackCardCatalog.IsYgoBlockedFromMultiplayerProceduralPools(player, c))
            .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Uniform pick; empty pool yields null.</summary>
    public static CardModel? PickOneUniform(Rng rng, IReadOnlyList<CardModel> pool)
    {
        if (pool.Count == 0)
            return null;
        return pool[rng.NextInt(0, pool.Count)];
    }

    /// <summary>
    /// Prefer Rare, then Uncommon, then Common; within the first tier that must be partially taken, shuffle with <paramref name="rng"/> and take the needed count.
    /// </summary>
    public static List<CardModel> ApplyBonusBulkCap(IReadOnlyList<CardModel> candidates, int maxCount, Rng rng)
    {
        if (candidates.Count == 0 || maxCount <= 0)
            return [];

        if (candidates.Count <= maxCount)
            return candidates.ToList();

        var rare = new List<CardModel>();
        var uncommon = new List<CardModel>();
        var common = new List<CardModel>();
        foreach (CardModel c in candidates)
        {
            switch (c.Rarity)
            {
                case CardRarity.Rare:
                    rare.Add(c);
                    break;
                case CardRarity.Uncommon:
                    uncommon.Add(c);
                    break;
                default:
                    common.Add(c);
                    break;
            }
        }

        var result = new List<CardModel>(maxCount);
        int remaining = maxCount;
        TakeTier(rare, ref remaining, result, rng);
        if (remaining > 0)
            TakeTier(uncommon, ref remaining, result, rng);
        if (remaining > 0)
            TakeTier(common, ref remaining, result, rng);
        return result;
    }

    private static void TakeTier(List<CardModel> tier, ref int remaining, List<CardModel> result, Rng rng)
    {
        if (remaining <= 0 || tier.Count == 0)
            return;

        int take = Math.Min(remaining, tier.Count);
        if (take == tier.Count)
        {
            result.AddRange(tier.OrderBy(c => c.Id.Entry, StringComparer.Ordinal));
            remaining -= take;
            return;
        }

        var copy = tier.OrderBy(c => c.Id.Entry, StringComparer.Ordinal).ToList();
        Shuffle(copy, rng);
        for (int i = 0; i < take; i++)
            result.Add(copy[i]);
        remaining -= take;
    }

    private static void Shuffle<T>(IList<T> list, Rng rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// One resolved bulk mate template per merchant row: first caller populates; later callers get the same instance (or none).
    /// </summary>
    public static bool TryGetMerchantBulkMateTemplate(
        MerchantCardEntry entry,
        Player player,
        YgoDuelistCard ygoTemplate,
        YgoCardPackTags discoveryTagMask,
        out CardModel? mateTemplate)
    {
        mateTemplate = null;
        if (MerchantBulkMateByEntry.TryGetValue(entry, out object? boxed))
        {
            if (ReferenceEquals(boxed, NoBulkMateSentinel))
                return false;
            mateTemplate = (CardModel)boxed;
            return true;
        }

        List<CardModel> pool = GetEligibleBulkTemplates(player, ygoTemplate, discoveryTagMask);
        if (pool.Count == 0)
        {
            MerchantBulkMateByEntry.Add(entry, NoBulkMateSentinel);
            return false;
        }

        CardModel? pick = PickOneUniform(player.PlayerRng.Shops, pool);
        if (pick == null)
        {
            MerchantBulkMateByEntry.Add(entry, NoBulkMateSentinel);
            return false;
        }

        MerchantBulkMateByEntry.Add(entry, pick);
        mateTemplate = pick;
        return true;
    }
}
