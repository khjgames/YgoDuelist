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
/// 18-card merchant (3×6 grid): 6C / 8U / 4R. Four packs — double-tag 5+5, single-tag 4+4 — row-major indices (col = idx%6, row = idx/6).
/// Row0: DoubleFirst cols 0–4, DoubleSecond col5. Row1: DoubleSecond cols 0–3, SingleTagFirst cols 4–5. Row2: SingleTagFirst cols 0–1, SingleTagSecond cols 2–5.
/// </summary>
public static class YgoMerchantOfferGenerator
{
    public const int SlotCount = 18;
    public const int GridColumns = 6;
    public const int GridRows = 3;

    private static readonly YgoCardPackTags HistogramIgnore =
        YgoCardPackTags.None | YgoCardPackTags.Starter | YgoCardPackTags.Bundled;

    /// <summary>First double-tag pack: top row, left five (indices 0–4).</summary>
    private static readonly int[] DoubleFirstSlots = { 0, 1, 2, 3, 4 };

    /// <summary>Second double-tag pack: top-right (5) plus row1 cols 0–3 (6–9).</summary>
    private static readonly int[] DoubleSecondSlots = { 5, 6, 7, 8, 9 };

    /// <summary>First single-tag pack: row1 cols 4–5 and row2 cols 0–1 (10–13).</summary>
    private static readonly int[] SingleTagFirstSlots = { 10, 11, 12, 13 };

    /// <summary>Second single-tag pack: bottom row, right four (14–17).</summary>
    private static readonly int[] SingleTagSecondSlots = { 14, 15, 16, 17 };

    public sealed class ShopSlot
    {
        public required CardModel Template { get; init; }
        public required CardRarity Rarity { get; init; }
        public required YgoCardPackTags RowTagMask { get; init; }
        public required YgoMerchantShopPackRole PackRole { get; init; }
    }

    public sealed class ShopOffer
    {
        public required IReadOnlyList<ShopSlot> Slots { get; init; }
        public required IReadOnlyList<YgoCardPackTags> PackMasks { get; init; }
    }

    public static ShopOffer Generate(Player player, Rng rng)
    {
        YgoCardPackTags affinityUnion = BuildAffinityUnion(player);
        (YgoCardPackTags d0, YgoCardPackTags d1, YgoCardPackTags t0, YgoCardPackTags t1) = RollFourMerchantPackMasks(rng);

        if (affinityUnion != YgoCardPackTags.None
            && !IntersectsAny(affinityUnion, d0, d1, t0, t1))
        {
            List<YgoCardPackTags> eligibleMains = YgoPackCardCatalog.PackThemeMainTags
                .Where(t => (t & affinityUnion) != 0)
                .ToList();
            if (eligibleMains.Count > 0)
            {
                YgoCardPackTags pick = eligibleMains[rng.NextInt(eligibleMains.Count)];
                int which = rng.NextInt(2);
                if (which == 0)
                    t0 = pick;
                else
                    t1 = pick;
            }
        }

        var rarityPool = BuildShuffledRarityPool(rng);
        List<CardRarity> r0 = TakeAndShuffleSlice(rarityPool, rng, 0, 5);
        List<CardRarity> r1 = TakeAndShuffleSlice(rarityPool, rng, 5, 5);
        List<CardRarity> r2 = TakeAndShuffleSlice(rarityPool, rng, 10, 4);
        List<CardRarity> r3 = TakeAndShuffleSlice(rarityPool, rng, 14, 4);

        var trunkCounts = CountIds(PlayerRunTrunk.GetOrCreatePile(player).Cards);
        var relatedBonus = BuildRelatedBonus(player);
        var chosenIds = new HashSet<ModelId>();
        var grid = new ShopSlot?[SlotCount];

        FillPackAtIndices(player, rng, d0, r0, DoubleFirstSlots, chosenIds, trunkCounts, relatedBonus, grid,
            YgoMerchantShopPackRole.DoubleFirst);
        FillPackAtIndices(player, rng, d1, r1, DoubleSecondSlots, chosenIds, trunkCounts, relatedBonus, grid,
            YgoMerchantShopPackRole.DoubleSecond);
        FillPackAtIndices(player, rng, t0, r2, SingleTagFirstSlots, chosenIds, trunkCounts, relatedBonus, grid,
            YgoMerchantShopPackRole.SingleTagFirst);
        FillPackAtIndices(player, rng, t1, r3, SingleTagSecondSlots, chosenIds, trunkCounts, relatedBonus, grid,
            YgoMerchantShopPackRole.SingleTagSecond);

        var slots = new List<ShopSlot>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
        {
            if (grid[i] != null)
            {
                slots.Add(grid[i]!);
                continue;
            }

            CardModel? filler = PickFallbackAny(player, rng, chosenIds);
            if (filler == null)
                continue;
            chosenIds.Add(filler.Id);
            slots.Add(new ShopSlot
            {
                Template = filler,
                Rarity = filler.Rarity,
                RowTagMask = YgoCardPackTags.None,
                PackRole = YgoMerchantShopPackRole.Filler
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
                RowTagMask = YgoCardPackTags.None,
                PackRole = YgoMerchantShopPackRole.Filler
            });
        }

        Log.Info($"[YgoDuelist][Shop] generated | slots={slots.Count} | affinity={affinityUnion}");
        return new ShopOffer
        {
            Slots = slots,
            PackMasks = new[] { d0, d1, t0, t1 }
        };
    }

    private static bool IntersectsAny(YgoCardPackTags affinity, YgoCardPackTags a, YgoCardPackTags b, YgoCardPackTags c, YgoCardPackTags d) =>
        (affinity & a) != 0 || (affinity & b) != 0 || (affinity & c) != 0 || (affinity & d) != 0;

    private static List<CardRarity> BuildShuffledRarityPool(Rng rng)
    {
        var pool = new List<CardRarity>(SlotCount);
        for (int i = 0; i < 6; i++)
            pool.Add(CardRarity.Common);
        for (int i = 0; i < 8; i++)
            pool.Add(CardRarity.Uncommon);
        for (int i = 0; i < 4; i++)
            pool.Add(CardRarity.Rare);
        Shuffle(pool, rng);
        return pool;
    }

    private static List<CardRarity> TakeAndShuffleSlice(List<CardRarity> pool, Rng rng, int start, int length)
    {
        var slice = pool.GetRange(start, length);
        Shuffle(slice, rng);
        return slice;
    }

    private static void FillPackAtIndices(
        Player player,
        Rng rng,
        YgoCardPackTags mask,
        IReadOnlyList<CardRarity> rarities,
        int[] gridIndices,
        HashSet<ModelId> chosenIds,
        Dictionary<ModelId, int> trunkCounts,
        Dictionary<ModelId, int> relatedBonus,
        ShopSlot?[] grid,
        YgoMerchantShopPackRole packRole)
    {
        for (int i = 0; i < gridIndices.Length; i++)
        {
            int g = gridIndices[i];
            CardRarity rarity = rarities[i];
            bool excludeBundled = false;
            CardModel? pick = PickShopSlot(
                player,
                rng,
                mask,
                ref rarity,
                chosenIds,
                excludeBundled,
                trunkCounts,
                relatedBonus);
            if (pick == null)
            {
                Log.Warn($"[YgoDuelist][Shop] empty_pick grid={g} mask={mask} rarity={rarity}");
                continue;
            }

            chosenIds.Add(pick.Id);
            grid[g] = new ShopSlot
            {
                Template = pick,
                Rarity = rarity,
                RowTagMask = mask,
                PackRole = packRole
            };
        }
    }

    private static (YgoCardPackTags d0, YgoCardPackTags d1, YgoCardPackTags t0, YgoCardPackTags t1) RollFourMerchantPackMasks(Rng rng)
    {
        YgoCardPackTags d0 = RollShopPackTagMask(rng, MainCopy(), CombinedCopy(), 2);
        YgoCardPackTags d1 = RollShopPackTagMask(rng, MainCopy(), CombinedCopy(), 2);
        YgoCardPackTags t0 = RollShopPackTagMask(rng, MainCopy(), CombinedCopy(), 1);
        YgoCardPackTags t1 = RollShopPackTagMask(rng, MainCopy(), CombinedCopy(), 1);
        return (d0, d1, t0, t1);
    }

    private static List<YgoCardPackTags> MainCopy() =>
        YgoPackCardCatalog.PackThemeMainTags.ToList();

    private static List<YgoCardPackTags> CombinedCopy() =>
        YgoPackCardCatalog.PackThemeMainTags.Concat(YgoPackCardCatalog.PackThemeSubTags).ToList();

    private static YgoCardPackTags BuildAffinityUnion(Player player)
    {
        var counts = new Dictionary<YgoCardPackTags, int>();
        foreach (CardModel c in player.Deck.Cards)
        {
            if (c is not YgoDuelistCard y)
                continue;
            YgoCardPackTags tags = YgoPackCardCatalog.GetEffectivePackTags(y) & ~HistogramIgnore;
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

        void AddFromYgo(YgoDuelistCard ygo, int delta) =>
            YgoRelatedCardWeighting.AccumulateRelatedWeight(ygo, delta, d);

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
