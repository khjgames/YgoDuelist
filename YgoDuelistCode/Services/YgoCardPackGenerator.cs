using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>One generated pack before instances are cloned for the reward UI.</summary>
public sealed record PackTemplateRoll(YgoCardPackTags TagMask, List<CardModel> Templates);

/// <summary>
/// Builds three YGO card packs for a reward: shared rarity column, per-pack tag masks, weighted tag picks (fatigue + desire),
/// bundles, owed rare vouchers. Boss encounter rewards use <see cref="CardRarityOddsType.BossEncounter"/> for column slot 0 only;
/// remaining slots use <see cref="CardRarityOddsType.RegularEncounter"/>. Every pack uses the same slot count; tag count only widens the card pool.
/// </summary>
public static class YgoCardPackGenerator
{
    public const int MaxCardsPerPack = 10;

    public static List<PackTemplateRoll> GenerateThreePackTemplates(
        Player player,
        Rng rng,
        int slotCount,
        CardRarityOddsType oddsType)
    {
        slotCount = Math.Clamp(slotCount, 3, 6);
        var progress = YgoPackRewardProgress.For(player);
        int owedRareEntering = progress.OwedRareCardVouchers;
        Log.Info(
            $"[YgoDuelist][PackGen] phase=start_generate | owedRareVouchers={owedRareEntering} | slotCount={slotCount} | rarityOdds={oddsType}");

        var odds = new CardRarityOdds(rng);

        var rolledRarities = new CardRarity[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            if (progress.OwedRareCardVouchers > 0)
            {
                rolledRarities[i] = CardRarity.Rare;
                progress.OwedRareCardVouchers--;
            }
            else
            {
                CardRarityOddsType slotOdds =
                    oddsType == CardRarityOddsType.BossEncounter && i > 0
                        ? CardRarityOddsType.RegularEncounter
                        : oddsType;
                rolledRarities[i] = odds.Roll(slotOdds);
            }
        }

        int columnRareSlots = rolledRarities.Take(slotCount).Count(r => r == CardRarity.Rare);
        Log.Info(
            $"[YgoDuelist][PackGen] phase=after_shared_rarity_column | column={FormatRarityColumn(rolledRarities, slotCount)} | columnRareSlots={columnRareSlots} | owedRareVouchers={progress.OwedRareCardVouchers} (after pity slots consumed)");

        var workingMain = YgoPackCardCatalog.PackThemeMainTags.ToList();
        var workingCombined = YgoPackCardCatalog.PackThemeMainTags
            .Concat(YgoPackCardCatalog.PackThemeSubTags)
            .ToList();

        var packs = new List<PackTemplateRoll>(3);
        for (int p = 0; p < 3; p++)
        {
            YgoCardPackTags tagMask = RollPackTagMask(rng, workingMain, workingCombined, progress);
            List<CardModel> onePack = FillOnePack(
                player,
                rng,
                tagMask,
                rolledRarities,
                slotCount,
                progress);
            packs.Add(new PackTemplateRoll(tagMask, onePack));
            Log.Info(
                $"[YgoDuelist][PackGen] phase=after_pack_{p}_filled | tagMask={tagMask} | {SummarizePackRarities(onePack)} | cards={onePack.Count} | owedRareVouchers={progress.OwedRareCardVouchers}");
        }

        Log.Info(
            $"[YgoDuelist][PackGen] phase=end_generate | owedRareVouchers={progress.OwedRareCardVouchers} (exit; includes pool-miss + bundle-trim vouchers from this roll)");
        return packs;
    }

    private static string FormatRarityColumn(CardRarity[] column, int len) =>
        string.Join(
            "",
            column.Take(len).Select(static r => r switch
            {
                CardRarity.Common => "C",
                CardRarity.Uncommon => "U",
                CardRarity.Rare => "R",
                _ => "?"
            }));

    private static string SummarizePackRarities(IReadOnlyList<CardModel> cards)
    {
        int c = cards.Count(x => x.Rarity == CardRarity.Common);
        int u = cards.Count(x => x.Rarity == CardRarity.Uncommon);
        int r = cards.Count(x => x.Rarity == CardRarity.Rare);
        return $"C={c} U={u} R={r}";
    }

    private static YgoCardPackTags RollPackTagMask(
        Rng rng,
        List<YgoCardPackTags> workingMain,
        List<YgoCardPackTags> workingCombined,
        YgoPackRewardProgressState progress)
    {
        int r = rng.NextInt(100);
        int tagCount = r < 48 ? 1 : r < 85 ? 2 : 3;
        int bump = tagCount == 1 ? 4 : tagCount == 2 ? 3 : 2;
        YgoCardPackTags mask = YgoCardPackTags.None;

        if (workingMain.Count == 0)
        {
            const string msg =
                "[YgoDuelist][PackGen] PackThemeMainTags working list is empty; cannot roll pack themes.";
            Log.Error(msg);
            throw new InvalidOperationException(msg);
        }

        YgoCardPackTags first = PickWeightedPackTag(rng, workingMain, progress);
        workingMain.Remove(first);
        workingCombined.Remove(first);
        progress.ApplyPackTagPickBumpAndTetris(first, bump);
        mask |= first;

        for (int extra = 1; extra < tagCount && workingCombined.Count > 0; extra++)
        {
            YgoCardPackTags next = PickWeightedPackTag(rng, workingCombined, progress);
            workingMain.Remove(next);
            workingCombined.Remove(next);
            progress.ApplyPackTagPickBumpAndTetris(next, bump);
            mask |= next;
        }

        return mask;
    }

    private static YgoCardPackTags PickWeightedPackTag(
        Rng rng,
        List<YgoCardPackTags> candidates,
        YgoPackRewardProgressState progress)
    {
        if (candidates.Count == 0)
        {
            const string msg = "[YgoDuelist][PackGen] Weighted tag pick had zero candidates.";
            Log.Error(msg);
            throw new InvalidOperationException(msg);
        }

        if (candidates.Count == 1)
            return candidates[0];

        YgoCardPackTags? pick = rng.WeightedNextItem(candidates, t => PackTagSelectionWeight(t, progress));
        return pick ?? candidates[rng.NextInt(candidates.Count)];
    }

    private static float PackTagSelectionWeight(YgoCardPackTags singleBit, YgoPackRewardProgressState progress)
    {
        int d = progress.GetPackTagAccum(singleBit);
        float f = YgoPackTagFatigue.FatigueFactor(d);
        int w = YgoPackTagWeightConfig.GetWeightMultiplier(singleBit);
        return Math.Max(1e-6f, f * (w / 10f));
    }

    private static List<CardModel> FillOnePack(
        Player player,
        Rng rng,
        YgoCardPackTags tagMask,
        CardRarity[] rolledRarities,
        int packSlots,
        YgoPackRewardProgressState progress)
    {
        var cards = new List<CardModel>();
        bool excludeBundledTagFromPool = false;

        var trunkCounts = CountIds(PlayerRunTrunk.GetOrCreatePile(player).Cards);
        var relatedBonus = BuildRelatedBonus(player);

        var slotRarities = new CardRarity[packSlots];
        for (int i = 0; i < packSlots; i++)
            slotRarities[i] = rolledRarities[i];

        for (int slot = 0; slot < packSlots; slot++)
        {
            CardRarity rarity = slotRarities[slot];
            CardModel pick = PickForSlot(
                player,
                rng,
                tagMask,
                ref rarity,
                cards,
                excludeBundledTagFromPool,
                trunkCounts,
                relatedBonus,
                progress);

            cards.Add(pick);

            if (pick is YgoDuelistCard yPick && yPick.BundledCards.Length > 0)
                excludeBundledTagFromPool = true;
        }

        bool hadBundleAnchor = cards.Exists(c => c is YgoDuelistCard y && y.BundledCards.Length > 0);
        int rareBeforeBundle = CountRaresInPack(cards);
        ApplyBundleResolution(rng, cards);
        int rareAfterBundle = CountRaresInPack(cards);
        if (hadBundleAnchor && rareAfterBundle < rareBeforeBundle)
        {
            int trimOwed = rareBeforeBundle - rareAfterBundle;
            progress.OwedRareCardVouchers += trimOwed;
            Log.Info(
                $"[YgoDuelist][PackGen] bundle_trim_removed_rares | +{trimOwed} owedRareVoucher(s) | owedRareVouchers={progress.OwedRareCardVouchers}");
        }

        return cards;
    }

    /// <summary>
    /// After all slot rolls, inject bundle mates by replacing lowest-rarity non-bundled non-rare cards; grow if needed; trim at 10 by dropping random non-bundled rares (Packs_System design).
    /// </summary>
    private static void ApplyBundleResolution(Rng rng, List<CardModel> cards)
    {
        CardModel? anchor = null;
        YgoDuelistCard? yAnchor = null;
        foreach (CardModel c in cards)
        {
            if (c is YgoDuelistCard y && y.BundledCards.Length > 0)
            {
                anchor = c;
                yAnchor = y;
                break;
            }
        }

        if (anchor == null || yAnchor == null)
            return;

        HashSet<ModelId> bundleIds = CollectBundleIds(anchor, yAnchor);

        foreach (CardModel mate in EnumerateBundleMatesExceptAnchor(anchor, yAnchor))
        {
            bool allowDupSelf = yAnchor.BundleGrantsExtraCopyOfSelf && mate.Id == anchor.Id;
            if (cards.Exists(c => c.Id == mate.Id) && !allowDupSelf)
                continue;

            int victim = FindLowestRarityNonBundledNonRareVictimIndex(cards, bundleIds);
            if (victim >= 0)
                cards[victim] = mate;
            else
                cards.Add(mate);

            TrimExceededMaxPackSize(cards, bundleIds, rng);
        }
    }

    private static HashSet<ModelId> CollectBundleIds(CardModel anchor, YgoDuelistCard y)
    {
        var set = new HashSet<ModelId> { anchor.Id };
        foreach (Type bt in y.BundledCards)
        {
            if (bt == anchor.GetType())
                continue;
            CardModel mate = YgoPackCardCatalog.CardFromType(bt);
            set.Add(mate.Id);
        }

        return set;
    }

    private static IEnumerable<CardModel> EnumerateBundleMatesExceptAnchor(CardModel anchor, YgoDuelistCard y)
    {
        foreach (Type bt in y.BundledCards)
        {
            if (bt == anchor.GetType() && !y.BundleGrantsExtraCopyOfSelf)
                continue;
            yield return YgoPackCardCatalog.CardFromType(bt);
        }
    }

    private static int NonRareRaritySortKey(CardRarity r) =>
        r switch
        {
            CardRarity.Common => 0,
            CardRarity.Uncommon => 1,
            _ => 99
        };

    private static int FindLowestRarityNonBundledNonRareVictimIndex(List<CardModel> cards, HashSet<ModelId> bundleIds)
    {
        int best = -1;
        int bestKey = 999;
        for (int i = 0; i < cards.Count; i++)
        {
            CardModel c = cards[i];
            if (bundleIds.Contains(c.Id))
                continue;
            if (c.Rarity == CardRarity.Rare)
                continue;

            int key = NonRareRaritySortKey(c.Rarity);
            if (key < bestKey)
            {
                bestKey = key;
                best = i;
            }
        }

        return best;
    }

    private static void TrimExceededMaxPackSize(List<CardModel> cards, HashSet<ModelId> bundleIds, Rng rng)
    {
        while (cards.Count > MaxCardsPerPack)
        {
            var rareNonBundled = new List<int>();
            for (int i = 0; i < cards.Count; i++)
            {
                if (!bundleIds.Contains(cards[i].Id) && cards[i].Rarity == CardRarity.Rare)
                    rareNonBundled.Add(i);
            }

            if (rareNonBundled.Count == 0)
                break;

            int pick = rareNonBundled[rng.NextInt(rareNonBundled.Count)];
            cards.RemoveAt(pick);
        }
    }

    private static CardModel PickForSlot(
        Player player,
        Rng rng,
        YgoCardPackTags tagMask,
        ref CardRarity rarity,
        List<CardModel> chosenSoFar,
        bool excludeBundledTagFromPool,
        Dictionary<ModelId, int> trunkCounts,
        Dictionary<ModelId, int> relatedBonus,
        YgoPackRewardProgressState progress)
    {
        List<CardModel>? pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool);

        if (pool == null || pool.Count == 0)
        {
            if (rarity == CardRarity.Rare)
            {
                rarity = CardRarity.Uncommon;
                pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool);
                if (pool == null || pool.Count == 0)
                {
                    rarity = CardRarity.Common;
                    pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool);
                }

                if (pool != null && pool.Count > 0)
                {
                    progress.OwedRareCardVouchers++;
                    Log.Info(
                        $"[YgoDuelist][PackGen] rare_slot_pool_empty_demoted | owedRareVouchers={progress.OwedRareCardVouchers} (+1 voucher, rare pool had no cards)");
                }
            }
            else if (rarity == CardRarity.Uncommon)
            {
                rarity = CardRarity.Common;
                pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool);
            }
        }

        if ((pool == null || pool.Count == 0) && rarity == CardRarity.Common)
        {
            rarity = CardRarity.Uncommon;
            pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool);
        }

        if (pool == null || pool.Count == 0)
        {
            string msg =
                $"[YgoDuelist][PackGen] No cards for pack tags {tagMask} after rarity demotion (final rarity {rarity}). " +
                "Add unlocked cards that match this theme, or fix PackTags on existing cards.";
            Log.Error(msg);
            throw new InvalidOperationException(msg);
        }

        CardModel? pick = rng.WeightedNextItem(pool, m =>
            Math.Max(1f, CalculateWeight(m!, chosenSoFar, trunkCounts, relatedBonus)));
        return pick ?? pool[rng.NextInt(pool.Count)];
    }

    private static List<CardModel>? BuildPool(
        Player player,
        YgoCardPackTags tagMask,
        CardRarity rarity,
        bool excludeBundledTagFromPool)
    {
        List<CardModel> raw = YgoPackCardCatalog.GetUnlockedPool(player, tagMask);
        return FilterPool(raw, rarity, excludeBundledTagFromPool);
    }

    private static List<CardModel>? FilterPool(List<CardModel> raw, CardRarity rarity, bool excludeBundledTagFromPool)
    {
        IEnumerable<CardModel> q = raw.Where(c => c.Rarity == rarity);
        if (excludeBundledTagFromPool)
            q = q.Where(c => c is not YgoDuelistCard y || (y.PackTags & YgoCardPackTags.Bundled) == 0);
        return q.ToList();
    }

    private static int CountRaresInPack(List<CardModel> cards) =>
        cards.Count(c => c.Rarity == CardRarity.Rare);

    private static float CalculateWeight(
        CardModel model,
        List<CardModel> chosenSoFar,
        Dictionary<ModelId, int> trunkCounts,
        Dictionary<ModelId, int> relatedBonus)
    {
        const int baseWeight = 20;
        float w = baseWeight * GetPackWeightMultiplier(model);
        w -= 2 * trunkCounts.GetValueOrDefault(model.Id, 0);
        w = Math.Max(1f, w);
        w += relatedBonus.GetValueOrDefault(model.Id, 0);

        int copiesInPack = chosenSoFar.Count(c => c.Id == model.Id);
        if (copiesInPack > 0)
            w /= MathF.Pow(3f, copiesInPack);

        return Math.Max(1f, w);
    }

    private static float GetPackWeightMultiplier(CardModel model) =>
        model is YgoDuelistCard y ? y.PackWeightMultiplier : 1f;

    private static Dictionary<ModelId, int> CountIds(IEnumerable<CardModel> cards)
    {
        var d = new Dictionary<ModelId, int>();
        foreach (CardModel c in cards)
        {
            d[c.Id] = d.GetValueOrDefault(c.Id, 0) + 1;
        }

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
}
