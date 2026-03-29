using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Builds three YGO card packs for a reward: shared rarity column, per-pack tag masks, weighted picks, bundles, owed rare vouchers.
/// </summary>
public static class YgoCardPackGenerator
{
    public const int MaxCardsPerPack = 10;

    public static List<List<CardModel>> GenerateThreePackTemplates(
        Player player,
        Rng rng,
        int slotCount,
        CardRarityOddsType oddsType)
    {
        slotCount = Math.Clamp(slotCount, 2, 6);
        var progress = YgoPackRewardProgress.For(player);
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
                rolledRarities[i] = odds.Roll(oddsType);
            }
        }

        var workingMain = YgoPackCardCatalog.PackThemeMainTags.ToList();
        var workingCombined = YgoPackCardCatalog.PackThemeMainTags
            .Concat(YgoPackCardCatalog.PackThemeSubTags)
            .ToList();

        var packs = new List<List<CardModel>>(3);
        for (int p = 0; p < 3; p++)
        {
            YgoCardPackTags tagMask = RollPackTagMask(rng, workingMain, workingCombined);
            packs.Add(FillOnePack(player, rng, tagMask, rolledRarities, slotCount, progress));
        }

        return packs;
    }

    private static YgoCardPackTags RollPackTagMask(Rng rng, List<YgoCardPackTags> workingMain, List<YgoCardPackTags> workingCombined)
    {
        int r = rng.NextInt(100);
        int tagCount = r < 48 ? 1 : r < 85 ? 2 : 3;
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

    private static List<CardModel> FillOnePack(
        Player player,
        Rng rng,
        YgoCardPackTags tagMask,
        CardRarity[] rolledRarities,
        int slotCount,
        YgoPackRewardProgressState progress)
    {
        var cards = new List<CardModel>();
        bool excludeBundledTagFromPool = false;

        var trunkCounts = CountIds(PlayerRunTrunk.GetOrCreatePile(player).Cards);
        var relatedBonus = BuildRelatedBonus(player);

        for (int slot = 0; slot < slotCount; slot++)
        {
            CardRarity rarity = rolledRarities[slot];
            CardModel? pick = PickForSlot(
                player,
                rng,
                tagMask,
                ref rarity,
                cards,
                excludeBundledTagFromPool,
                trunkCounts,
                relatedBonus,
                progress);

            if (pick == null)
                continue;

            cards.Add(pick);

            if (pick is YgoDuelistCard yPick && yPick.BundledCards.Length > 0)
                excludeBundledTagFromPool = true;
        }

        bool hadBundleAnchor = cards.Exists(c => c is YgoDuelistCard y && y.BundledCards.Length > 0);
        int rareBeforeBundle = CountRaresInPack(cards);
        ApplyBundleResolution(rng, cards);
        int rareAfterBundle = CountRaresInPack(cards);
        if (hadBundleAnchor && rareAfterBundle < rareBeforeBundle)
            progress.OwedRareCardVouchers += rareBeforeBundle - rareAfterBundle;

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
            if (cards.Exists(c => c.Id == mate.Id))
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
            set.Add(YgoPackCardCatalog.CardFromType(bt).Id);

        return set;
    }

    private static IEnumerable<CardModel> EnumerateBundleMatesExceptAnchor(CardModel anchor, YgoDuelistCard y)
    {
        foreach (Type bt in y.BundledCards)
        {
            if (bt == anchor.GetType())
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

    private static CardModel? PickForSlot(
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
                    progress.OwedRareCardVouchers++;
            }
            else if (rarity == CardRarity.Uncommon)
            {
                rarity = CardRarity.Common;
                pool = BuildPool(player, tagMask, rarity, excludeBundledTagFromPool);
            }
        }

        if (pool == null || pool.Count == 0)
            pool = BuildPoolIgnoreTag(player, rarity, excludeBundledTagFromPool);

        if (pool == null || pool.Count == 0)
            return YgoPackCardCatalog.GetUnlockedPool(player, tagMask).FirstOrDefault();

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

    private static List<CardModel>? BuildPoolIgnoreTag(Player player, CardRarity rarity, bool excludeBundledTagFromPool)
    {
        HashSet<ModelId> unlocked = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(c => c.Id)
            .ToHashSet();
        List<CardModel> raw = YgoPackCardCatalog.GetAllYgoTemplates().Where(c => unlocked.Contains(c.Id)).ToList();
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
        float w = baseWeight;
        w -= 2 * trunkCounts.GetValueOrDefault(model.Id, 0);
        w = Math.Max(1f, w);
        w += relatedBonus.GetValueOrDefault(model.Id, 0);

        int copiesInPack = chosenSoFar.Count(c => c.Id == model.Id);
        if (copiesInPack > 0)
            w /= MathF.Pow(3f, copiesInPack);

        return Math.Max(1f, w);
    }

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
                    // ignore bad related entries
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
}
