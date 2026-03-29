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
        var chosen = new List<CardModel>();
        var bundleMemberIds = new HashSet<ModelId>();
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
                chosen,
                excludeBundledTagFromPool,
                trunkCounts,
                relatedBonus,
                progress);

            if (pick == null)
                continue;

            chosen.Add(pick);

            if (pick is YgoDuelistCard yPick && yPick.BundledCards.Length > 0)
            {
                excludeBundledTagFromPool = true;
                foreach (Type bt in yPick.BundledCards)
                {
                    if (bt == pick.GetType())
                        continue;
                    CardModel mate = YgoPackCardCatalog.CardFromType(bt);
                    if (chosen.All(c => c.Id != mate.Id))
                    {
                        chosen.Add(mate);
                        bundleMemberIds.Add(mate.Id);
                    }
                }

                bundleMemberIds.Add(pick.Id);
            }

            EnforceMaxPackSize(chosen, bundleMemberIds);
        }

        return chosen;
    }

    private static void EnforceMaxPackSize(List<CardModel> chosen, HashSet<ModelId> bundleMemberIds)
    {
        while (chosen.Count > MaxCardsPerPack)
        {
            int idx = chosen.FindIndex(c =>
                !bundleMemberIds.Contains(c.Id) && c.Rarity != CardRarity.Rare);
            if (idx < 0)
                idx = chosen.FindIndex(c => !bundleMemberIds.Contains(c.Id));
            if (idx < 0)
            {
                chosen.RemoveAt(chosen.Count - 1);
                continue;
            }

            chosen.RemoveAt(idx);
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

        CardModel? pick = rng.WeightedNextItem(pool, m => CalculateWeight(m!, chosenSoFar, trunkCounts, relatedBonus));
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
