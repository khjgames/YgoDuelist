using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// 16-card merchant offer: 4 themed rows — exactly 2 single-tag and 2 double-tag pack masks (shuffled rows),
/// fixed 6/8/2 rarity mix, deck top-2 tag affinity on at least one row.
/// </summary>
public static class YgoMerchantOfferGenerator
{
    public const int SlotCount = 16;
    public const int RowCount = 4;

    private static readonly YgoCardPackTags HistogramIgnore =
        YgoCardPackTags.None | YgoCardPackTags.Starter | YgoCardPackTags.Bundled;

    public sealed class ShopSlot
    {
        public required CardModel Template { get; init; }
        public required CardRarity Rarity { get; init; }
        public required YgoCardPackTags RowTagMask { get; init; }
    }

    public sealed class ShopOffer
    {
        public required IReadOnlyList<ShopSlot> Slots { get; init; }
        public required IReadOnlyList<YgoCardPackTags> RowMasks { get; init; }
    }

    public static ShopOffer Generate(Player player, Rng rng)
    {
        YgoCardPackTags affinityUnion = BuildAffinityUnion(player);
        var rowMasks = RollMerchantRowMasks(rng);

        if (affinityUnion != YgoCardPackTags.None && !rowMasks.Any(m => (m & affinityUnion) != 0))
        {
            var eligibleMains = YgoPackCardCatalog.PackThemeMainTags
                .Where(t => (t & affinityUnion) != 0)
                .ToList();
            if (eligibleMains.Count > 0)
            {
                var singleRowIndices = Enumerable.Range(0, RowCount)
                    .Where(i => CountPackThemeBits(rowMasks[i]) == 1)
                    .ToList();
                int rowIdx = singleRowIndices.Count > 0
                    ? singleRowIndices[rng.NextInt(singleRowIndices.Count)]
                    : rng.NextInt(RowCount);
                rowMasks[rowIdx] = eligibleMains[rng.NextInt(eligibleMains.Count)];
            }
        }

        List<CardRarity> rarities = AssignRaritiesBestFit(player, rowMasks, rng);

        var trunkCounts = CountIds(PlayerRunTrunk.GetOrCreatePile(player).Cards);
        var relatedBonus = BuildRelatedBonus(player);
        var chosenIds = new HashSet<ModelId>();
        var slots = new List<ShopSlot>(SlotCount);

        for (int i = 0; i < SlotCount; i++)
        {
            int row = i / 4;
            YgoCardPackTags rowMask = rowMasks[row];
            CardRarity rarity = rarities[i];
            bool excludeBundled = false;
            CardModel? pick = PickShopSlot(
                player,
                rng,
                rowMask,
                ref rarity,
                chosenIds,
                excludeBundled,
                trunkCounts,
                relatedBonus);
            if (pick == null)
            {
                Log.Warn($"[YgoDuelist][Shop] empty_pick slot={i} row={row} mask={rowMask} rarity={rarity}");
                continue;
            }

            chosenIds.Add(pick.Id);
            slots.Add(new ShopSlot
            {
                Template = pick,
                Rarity = rarity,
                RowTagMask = rowMask
            });
        }

        while (slots.Count < SlotCount)
        {
            CardModel? filler = PickFallbackAny(player, rng, chosenIds);
            if (filler == null)
                break;
            chosenIds.Add(filler.Id);
            slots.Add(new ShopSlot
            {
                Template = filler,
                Rarity = filler.Rarity,
                RowTagMask = YgoCardPackTags.None
            });
        }

        Log.Info($"[YgoDuelist][Shop] generated | slots={slots.Count} | affinity={affinityUnion}");
        return new ShopOffer
        {
            Slots = slots,
            RowMasks = rowMasks
        };
    }

    private static YgoCardPackTags BuildAffinityUnion(Player player)
    {
        var counts = new Dictionary<YgoCardPackTags, int>();
        foreach (CardModel c in player.Deck.Cards)
        {
            if (c is not YgoDuelistCard y)
                continue;
            YgoCardPackTags tags = y.PackTags & ~HistogramIgnore;
            foreach (YgoCardPackTags bit in EnumerateThemeBits(tags))
                counts[bit] = counts.GetValueOrDefault(bit, 0) + 1;
        }

        if (counts.Count == 0)
            return YgoCardPackTags.None;

        var ordered = counts
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => (long)kv.Key)
            .ToList();

        YgoCardPackTags a = ordered[0].Key;
        if (ordered.Count == 1)
            return a;

        int top = ordered[0].Value;
        var tied = ordered.TakeWhile(kv => kv.Value == top).Select(kv => kv.Key).ToList();
        if (tied.Count >= 2)
        {
            tied.Sort((x, y) => ((long)y).CompareTo((long)x));
            return tied[0] | tied[1];
        }

        return a | ordered[1].Key;
    }

    private static IEnumerable<YgoCardPackTags> EnumerateThemeBits(YgoCardPackTags mask)
    {
        foreach (YgoCardPackTags t in YgoPackCardCatalog.PackThemeMainTags)
        {
            if ((mask & t) != 0)
                yield return t;
        }

        foreach (YgoCardPackTags t in YgoPackCardCatalog.PackThemeSubTags)
        {
            if ((mask & t) != 0)
                yield return t;
        }
    }

    /// <summary>Exactly two rows use one theme bit, two rows use two bits; row order is random.</summary>
    private static YgoCardPackTags[] RollMerchantRowMasks(Rng rng)
    {
        var rowMasks = new YgoCardPackTags[RowCount];
        var tagCounts = new[] { 1, 1, 2, 2 };
        Shuffle(tagCounts, rng);

        for (int row = 0; row < RowCount; row++)
        {
            var main = YgoPackCardCatalog.PackThemeMainTags.ToList();
            var combined = YgoPackCardCatalog.PackThemeMainTags.Concat(YgoPackCardCatalog.PackThemeSubTags).ToList();
            rowMasks[row] = RollShopPackTagMask(rng, main, combined, tagCounts[row]);
        }

        return rowMasks;
    }

    private static int CountPackThemeBits(YgoCardPackTags mask) =>
        EnumerateThemeBits(mask).Count();

    private static YgoCardPackTags RollShopPackTagMask(
        Rng rng,
        List<YgoCardPackTags> workingMain,
        List<YgoCardPackTags> workingCombined,
        int tagCount)
    {
        tagCount = Math.Clamp(tagCount, 1, 3);
        YgoCardPackTags mask = YgoCardPackTags.None;

        if (workingMain.Count == 0)
            return YgoCardPackTags.Dragon;

        YgoCardPackTags first = rng.NextItem(workingMain);
        workingMain.Remove(first);
        workingCombined.Remove(first);
        mask |= first;

        for (int extra = 1; extra < tagCount && workingCombined.Count > 0; extra++)
        {
            YgoCardPackTags next = rng.NextItem(workingCombined);
            workingMain.Remove(next);
            workingCombined.Remove(next);
            mask |= next;
        }

        return mask;
    }

    private static void Shuffle<T>(IList<T> list, Rng rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private const int TargetShopCommons = 6;
    private const int TargetShopUncommons = 8;
    private const int TargetShopRares = 2;

    /// <summary>
    /// Partitions 6C/8U/2R across four rows of four slots so global counts match and per-row assignment
    /// minimizes shortage vs that row's unlocked tag pool (same eligibility as <see cref="FilterPool"/> without dedup).
    /// Within each row, slot order is shuffled.
    /// </summary>
    private static List<CardRarity> AssignRaritiesBestFit(Player player, YgoCardPackTags[] rowMasks, Rng rng)
    {
        var avail = new (int C, int U, int R)[RowCount];
        for (int row = 0; row < RowCount; row++)
            avail[row] = CountEligiblePerRarityForRow(player, rowMasks[row]);

        if (!TrySolveRowRarityPartition(avail, rng, out (int C, int U, int R)[] rowMix))
            return BuildShuffledGlobalRarityList(rng);

        var rarities = new List<CardRarity>(SlotCount);
        for (int row = 0; row < RowCount; row++)
        {
            var rowList = new List<CardRarity>(4);
            for (int i = 0; i < rowMix[row].C; i++)
                rowList.Add(CardRarity.Common);
            for (int i = 0; i < rowMix[row].U; i++)
                rowList.Add(CardRarity.Uncommon);
            for (int i = 0; i < rowMix[row].R; i++)
                rowList.Add(CardRarity.Rare);
            Shuffle(rowList, rng);
            rarities.AddRange(rowList);
        }

        return rarities;
    }

    private static List<CardRarity> BuildShuffledGlobalRarityList(Rng rng)
    {
        var rarities = new List<CardRarity>(SlotCount);
        for (int i = 0; i < TargetShopCommons; i++)
            rarities.Add(CardRarity.Common);
        for (int i = 0; i < TargetShopUncommons; i++)
            rarities.Add(CardRarity.Uncommon);
        for (int i = 0; i < TargetShopRares; i++)
            rarities.Add(CardRarity.Rare);
        Shuffle(rarities, rng);
        return rarities;
    }

    private static (int C, int U, int R) CountEligiblePerRarityForRow(Player player, YgoCardPackTags tagMask)
    {
        List<CardModel> raw = YgoPackCardCatalog.GetUnlockedPool(player, tagMask);
        int c = 0, u = 0, r = 0;
        foreach (CardModel model in raw)
        {
            if (!AllowedForRun(player, model))
                continue;
            switch (model.Rarity)
            {
                case CardRarity.Common:
                    c++;
                    break;
                case CardRarity.Uncommon:
                    u++;
                    break;
                case CardRarity.Rare:
                    r++;
                    break;
            }
        }

        return (c, u, r);
    }

    /// <summary>
    /// Penalize assigning more of a rarity than exists in the row pool (drives demotion in <see cref="PickShopSlot"/>).
    /// Rare over-assign is weighted higher than uncommon/common.
    /// </summary>
    private static int RowShortfallCost((int C, int U, int R) avail, int c, int u, int rare)
    {
        int dC = Math.Max(0, c - avail.C);
        int dU = Math.Max(0, u - avail.U);
        int dR = Math.Max(0, rare - avail.R);
        return dC + 3 * dU + 12 * dR;
    }

    private static bool TrySolveRowRarityPartition(
        (int C, int U, int R)[] avail,
        Rng rng,
        out (int C, int U, int R)[] rowMix)
    {
        rowMix = new (int C, int U, int R)[RowCount];
        const int inf = int.MaxValue / 8;
        var dp = new int[RowCount + 1, TargetShopCommons + 1, TargetShopUncommons + 1, TargetShopRares + 1];
        for (int a = 0; a <= RowCount; a++)
        for (int gc = 0; gc <= TargetShopCommons; gc++)
        for (int gu = 0; gu <= TargetShopUncommons; gu++)
        for (int gr = 0; gr <= TargetShopRares; gr++)
            dp[a, gc, gu, gr] = inf;

        dp[0, 0, 0, 0] = 0;

        for (int row = 0; row < RowCount; row++)
        {
            for (int gc = 0; gc <= TargetShopCommons; gc++)
            for (int gu = 0; gu <= TargetShopUncommons; gu++)
            for (int gr = 0; gr <= TargetShopRares; gr++)
            {
                int cur = dp[row, gc, gu, gr];
                if (cur >= inf)
                    continue;

                for (int c = 0; c <= 4; c++)
                for (int u = 0; u <= 4 - c; u++)
                {
                    int rare = 4 - c - u;
                    int ngc = gc + c;
                    int ngu = gu + u;
                    int ngr = gr + rare;
                    if (ngc > TargetShopCommons || ngu > TargetShopUncommons || ngr > TargetShopRares)
                        continue;

                    int nextCost = cur + RowShortfallCost(avail[row], c, u, rare);
                    if (nextCost < dp[row + 1, ngc, ngu, ngr])
                        dp[row + 1, ngc, ngu, ngr] = nextCost;
                }
            }
        }

        int finalCost = dp[RowCount, TargetShopCommons, TargetShopUncommons, TargetShopRares];
        if (finalCost >= inf)
            return false;

        int gC = TargetShopCommons;
        int gU = TargetShopUncommons;
        int gR = TargetShopRares;

        for (int row = RowCount - 1; row >= 0; row--)
        {
            var picks = new List<(int c, int u, int rare)>();
            for (int c = 0; c <= 4; c++)
            for (int u = 0; u <= 4 - c; u++)
            {
                int rare = 4 - c - u;
                int pC = gC - c;
                int pU = gU - u;
                int pR = gR - rare;
                if (pC < 0 || pU < 0 || pR < 0)
                    continue;
                int prev = dp[row, pC, pU, pR];
                if (prev >= inf)
                    continue;
                if (prev + RowShortfallCost(avail[row], c, u, rare) != dp[row + 1, gC, gU, gR])
                    continue;
                picks.Add((c, u, rare));
            }

            if (picks.Count == 0)
                return false;

            (int c, int u, int rare) chosen = picks[rng.NextInt(picks.Count)];
            rowMix[row] = (chosen.c, chosen.u, chosen.rare);
            gC -= chosen.c;
            gU -= chosen.u;
            gR -= chosen.rare;
        }

        return true;
    }

    private static CardModel? PickShopSlot(
        Player player,
        Rng rng,
        YgoCardPackTags tagMask,
        ref CardRarity rarity,
        HashSet<ModelId> chosenIds,
        bool excludeBundledTagFromPool,
        Dictionary<ModelId, int> trunkCounts,
        Dictionary<ModelId, int> relatedBonus)
    {
        List<CardModel>? pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool, chosenIds);

        if (pool == null || pool.Count == 0)
        {
            if (rarity == CardRarity.Rare)
            {
                rarity = CardRarity.Uncommon;
                pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool, chosenIds);
                if (pool == null || pool.Count == 0)
                {
                    rarity = CardRarity.Common;
                    pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool, chosenIds);
                }
            }
            else if (rarity == CardRarity.Uncommon)
            {
                rarity = CardRarity.Common;
                pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool, chosenIds);
            }
        }

        if (pool == null || pool.Count == 0)
            pool = BuildPoolIgnoreTag(player, rarity, excludeBundledTagFromPool, chosenIds);

        if (pool == null || pool.Count == 0)
        {
            List<CardModel> any = YgoPackCardCatalog.GetUnlockedPool(player, tagMask)
                .Where(c => !chosenIds.Contains(c.Id))
                .ToList();
            return any.Count > 0 ? rng.NextItem(any) : null;
        }

        var weightList = pool;
        CardModel? pick = rng.WeightedNextItem(weightList, m =>
            Math.Max(1f, CalculateWeight(m!, trunkCounts, relatedBonus, chosenIds)));
        return pick ?? pool[rng.NextInt(pool.Count)];
    }

    private static List<CardModel>? BuildPool(
        Player player,
        YgoCardPackTags tagMask,
        CardRarity rarity,
        bool excludeBundledTagFromPool,
        HashSet<ModelId> chosenIds)
    {
        List<CardModel> raw = YgoPackCardCatalog.GetUnlockedPool(player, tagMask);
        return FilterPool(player, raw, rarity, excludeBundledTagFromPool, chosenIds);
    }

    private static List<CardModel>? BuildPoolIgnoreTag(
        Player player,
        CardRarity rarity,
        bool excludeBundledTagFromPool,
        HashSet<ModelId> chosenIds)
    {
        HashSet<ModelId> unlocked = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(c => c.Id)
            .ToHashSet();
        List<CardModel> raw = YgoPackCardCatalog.GetAllYgoTemplates().Where(c => unlocked.Contains(c.Id)).ToList();
        return FilterPool(player, raw, rarity, excludeBundledTagFromPool, chosenIds);
    }

    private static List<CardModel>? FilterPool(
        Player player,
        List<CardModel> raw,
        CardRarity rarity,
        bool excludeBundledTagFromPool,
        HashSet<ModelId> chosenIds)
    {
        IEnumerable<CardModel> q = raw.Where(c =>
            c.Rarity == rarity && !chosenIds.Contains(c.Id) && AllowedForRun(player, c));
        if (excludeBundledTagFromPool)
            q = q.Where(c => c is not YgoDuelistCard y || (y.PackTags & YgoCardPackTags.Bundled) == 0);
        return q.ToList();
    }

    private static bool AllowedForRun(Player player, CardModel c)
    {
        if (player.RunState.Players.Count > 1)
            return c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly;
        return c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly;
    }

    private static float CalculateWeight(
        CardModel model,
        Dictionary<ModelId, int> trunkCounts,
        Dictionary<ModelId, int> relatedBonus,
        HashSet<ModelId> chosenIds)
    {
        const int baseWeight = 20;
        float w = baseWeight;
        w -= 2 * trunkCounts.GetValueOrDefault(model.Id, 0);
        w = Math.Max(1f, w);
        w += relatedBonus.GetValueOrDefault(model.Id, 0);
        if (chosenIds.Contains(model.Id))
            w = 0.01f;
        return Math.Max(1f, w);
    }

    private static Dictionary<ModelId, int> CountIds(IEnumerable<CardModel> cards)
    {
        var d = new Dictionary<ModelId, int>();
        foreach (CardModel c in cards)
            d[c.Id] = d.GetValueOrDefault(c.Id, 0) + 1;
        return d;
    }

    private static Dictionary<ModelId, int> BuildRelatedBonus(Player player)
    {
        var d = new Dictionary<ModelId, int>();

        void AddFromYgo(YgoDuelistCard ygo, int delta)
        {
            foreach (Type t in ygo.RelatedCards)
            {
                try
                {
                    CardModel related = YgoPackCardCatalog.CardFromType(t);
                    d[related.Id] = d.GetValueOrDefault(related.Id, 0) + delta;
                }
                catch
                {
                    // ignore
                }
            }
        }

        foreach (CardModel c in player.Deck.Cards)
        {
            if (c is YgoDuelistCard y)
                AddFromYgo(y, 2);
        }

        foreach (CardModel c in PlayerRunSideDeck.GetOrCreatePile(player).Cards)
        {
            if (c is YgoDuelistCard y)
                AddFromYgo(y, 1);
        }

        return d;
    }

    private static CardModel? PickFallbackAny(Player player, Rng rng, HashSet<ModelId> chosenIds)
    {
        HashSet<ModelId> unlocked = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(c => c.Id)
            .ToHashSet();
        List<CardModel> pool = YgoPackCardCatalog.GetAllYgoTemplates()
            .Where(c =>
                unlocked.Contains(c.Id) && c.Rarity != CardRarity.Basic && !chosenIds.Contains(c.Id)
                && AllowedForRun(player, c))
            .ToList();
        return pool.Count > 0 ? rng.NextItem(pool) : null;
    }
}
