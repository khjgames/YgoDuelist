using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Starter Neow grid bucket: Spell / Trap / monster by level band.
/// <list type="bullet">
/// <item><description><see cref="Mon_Low"/>: monster level 1–2.</description></item>
/// <item><description><see cref="Mon_Mid"/>: monster level 3–4.</description></item>
/// <item><description><see cref="Mon_High"/>: monster level ≥ 5.</description></item>
/// </list>
/// </summary>
public enum StarterCategory
{
    Spell,
    Trap,
    Mon_Low,
    Mon_Mid,
    Mon_High
}

/// <summary>
/// Cards with <see cref="YgoCardPackTags.Starter"/> for the pre-Neow starter grid.
/// Fills <see cref="GridSize"/> slots with fixed rarity order and fixed per-category counts, rotating through a shuffled <see cref="CategoryPrecedenceBase"/>.
/// </summary>
public static class YgoStarterCardCatalog
{
    public const int GridSize = 19;

    /// <summary>Default category rotation order before <see cref="Rng"/> shuffle (one full permutation per run).</summary>
    public static readonly StarterCategory[] CategoryPrecedenceBase =
    [
        StarterCategory.Spell,
        StarterCategory.Trap,
        StarterCategory.Mon_Low,
        StarterCategory.Mon_Mid,
        StarterCategory.Mon_High
    ];

    /// <summary>1 rare, 6 uncommons, 12 commons — applied in this sequence to the 19 grid slots.</summary>
    public static readonly CardRarity[] StarterCardRarities =
    [
        CardRarity.Rare, // 1 rare
        CardRarity.Uncommon, CardRarity.Uncommon, 
        CardRarity.Uncommon, CardRarity.Uncommon, 
        CardRarity.Uncommon, CardRarity.Uncommon, // 6 uncommons
        CardRarity.Common, CardRarity.Common, CardRarity.Common, 
        CardRarity.Common, CardRarity.Common, CardRarity.Common, 
        CardRarity.Common, CardRarity.Common, CardRarity.Common, 
        CardRarity.Common, CardRarity.Common, CardRarity.Common // 12 commons
    ];

    /// <summary>
    /// Remaining picks required per category (must sum to <see cref="GridSize"/>).
    /// Matches <c>Game_Design/Deck_Trunk_Side_System.md</c>: 3 Mon_High, 4 Mon_Mid, 4 Mon_Low, 4 Spell, 4 Trap.
    /// </summary>
    public static readonly IReadOnlyDictionary<StarterCategory, int> CategorySlotTotals =
        new Dictionary<StarterCategory, int>
        {
            [StarterCategory.Mon_High] = 3,
            [StarterCategory.Spell] = 4,
            [StarterCategory.Trap] = 4,
            [StarterCategory.Mon_Mid] = 4,
            [StarterCategory.Mon_Low] = 4
        };

    /// <summary>If a category has fewer than this many <see cref="CardRarity.Uncommon"/> starter types, diagnostics list every card in that bracket.</summary>
    private const int StarterDiagUncommonListBelow = 6;

    /// <summary>If a category has fewer than this many <see cref="CardRarity.Common"/> starter types, diagnostics list every card in that bracket.</summary>
    private const int StarterDiagCommonListBelow = 12;

    private static readonly object Gate = new();
    private static List<Type>? sStarterCardTypes;
    private static Dictionary<(StarterCategory, CardRarity), List<Type>>? sStarterBuckets;
    private static Dictionary<(StarterCategory, CardRarity), int>? sAvailabilitySnapshot;
    private static MethodInfo? sModelDbCardNoArg;

    static YgoStarterCardCatalog()
    {
        int sum = CategorySlotTotals.Values.Sum();
        if (sum != GridSize)
            throw new InvalidOperationException($"YgoStarterCardCatalog: category slot sum {sum} != {nameof(GridSize)} {GridSize}.");
        if (StarterCardRarities.Length != GridSize)
            throw new InvalidOperationException($"{nameof(StarterCardRarities)}.Length must equal {nameof(GridSize)}.");
    }

    /// <summary>Counts of Starter-tagged card <em>types</em> per category and rarity (canonical models).</summary>
    public static IReadOnlyDictionary<(StarterCategory Category, CardRarity Rarity), int> GetStarterAvailabilityCounts()
    {
        lock (Gate)
        {
            EnsureCatalogBuilt();
            return sAvailabilitySnapshot!;
        }
    }

    public static List<CardModel> CreateRandomGrid(Rng rng, int count)
    {
        if (count != GridSize)
            throw new InvalidOperationException($"{nameof(CreateRandomGrid)} requires count == {nameof(GridSize)} ({GridSize}); got {count}.");

        lock (Gate)
        {
            EnsureCatalogBuilt();
        }

        Dictionary<(StarterCategory, CardRarity), List<Type>> buckets = CloneBuckets(sStarterBuckets!);
        var remainingByCategory = new Dictionary<StarterCategory, int>(CategorySlotTotals);
        List<StarterCategory> categoryOrder = CategoryPrecedenceBase.ToList();
        categoryOrder.UnstableShuffle(rng);

        int cpCursor = -1;
        var pickedEntries = new HashSet<string>(StringComparer.Ordinal);
        var grid = new List<CardModel>(GridSize);

        for (int i = 0; i < GridSize; i++)
        {
            CardRarity selectedRarity = StarterCardRarities[i];
            CardModel card = GetNextUniqueCardOfRarity(
                selectedRarity,
                ref cpCursor,
                categoryOrder,
                remainingByCategory,
                buckets,
                pickedEntries,
                rng,
                recursionDepth: 0);
            CardModel mutable = card.ToMutable();
            mutable.FloorAddedToDeck = 1;
            grid.Add(mutable);
        }

        GD.Print($"[YgoDuelist NeowDraft] CreateRandomGrid: structured fill categoryOrder=[{string.Join(",", categoryOrder)}] distinctIds={grid.Select(c => c.Id.Entry).Distinct().Count()}");
        MainFile.Logger.Info($"[NeowDraft catalog] structured grid built distinctIds={grid.Select(c => c.Id.Entry).Distinct().Count()}");
        return grid;
    }

    private static CardModel GetNextUniqueCardOfRarity(
        CardRarity selectedRarity,
        ref int cpCursor,
        IReadOnlyList<StarterCategory> categoryPrecedenceOrder,
        Dictionary<StarterCategory, int> remainingByCategory,
        Dictionary<(StarterCategory, CardRarity), List<Type>> buckets,
        HashSet<string> pickedEntries,
        Rng rng,
        int recursionDepth)
    {
        if (recursionDepth > categoryPrecedenceOrder.Count)
        {
            throw new InvalidOperationException(
                $"Starter grid: cannot satisfy rarity {selectedRarity} — no category left with quota and an unpicked card of that rarity. " +
                $"Remaining quotas: {string.Join(", ", remainingByCategory.Select(kv => $"{kv.Key}={kv.Value}"))}.");
        }

        cpCursor++;
        if (cpCursor >= categoryPrecedenceOrder.Count)
            cpCursor = 0;

        StarterCategory selectedCategory = categoryPrecedenceOrder[cpCursor];

        if (remainingByCategory.GetValueOrDefault(selectedCategory, 0) <= 0)
            return GetNextUniqueCardOfRarity(selectedRarity, ref cpCursor, categoryPrecedenceOrder, remainingByCategory, buckets, pickedEntries, rng, recursionDepth + 1);

        if (!buckets.TryGetValue((selectedCategory, selectedRarity), out List<Type>? pool) || pool.Count == 0)
            return GetNextUniqueCardOfRarity(selectedRarity, ref cpCursor, categoryPrecedenceOrder, remainingByCategory, buckets, pickedEntries, rng, recursionDepth + 1);

        int idx = rng.NextInt(0, pool.Count);
        Type chosenType = pool[idx];
        CardModel canonical = CardFromType(chosenType);
        string entry = canonical.Id.Entry;
        if (pickedEntries.Contains(entry))
        {
            pool.RemoveAt(idx);
            return GetNextUniqueCardOfRarity(selectedRarity, ref cpCursor, categoryPrecedenceOrder, remainingByCategory, buckets, pickedEntries, rng, recursionDepth + 1);
        }

        pool.RemoveAt(idx);
        pickedEntries.Add(entry);
        remainingByCategory[selectedCategory]--;

        return canonical;
    }

    private static Dictionary<(StarterCategory, CardRarity), List<Type>> CloneBuckets(
        Dictionary<(StarterCategory, CardRarity), List<Type>> source)
    {
        var copy = new Dictionary<(StarterCategory, CardRarity), List<Type>>();
        foreach (KeyValuePair<(StarterCategory, CardRarity), List<Type>> kv in source)
            copy[kv.Key] = new List<Type>(kv.Value);
        return copy;
    }

    private static void EnsureCatalogBuilt()
    {
        if (sStarterCardTypes != null)
            return;

        var types = new List<Type>();
        var buckets = new Dictionary<(StarterCategory, CardRarity), List<Type>>();

        foreach (Type t in typeof(YgoDuelistCard).Assembly.GetTypes())
        {
            if (t.IsAbstract || !t.IsSubclassOf(typeof(YgoDuelistCard)))
                continue;

            CardModel model;
            try
            {
                model = CardFromType(t);
            }
            catch
            {
                continue;
            }

            if (model is not YgoDuelistCard ygo || (ygo.PackTags & YgoCardPackTags.Starter) == 0)
                continue;

            types.Add(t);
            StarterCategory cat = GetStarterCategory(model);
            CardRarity rarity = NormalizeStarterRarity(model.Rarity);
            var key = (cat, rarity);
            if (!buckets.TryGetValue(key, out List<Type>? list))
            {
                list = new List<Type>();
                buckets[key] = list;
            }

            list.Add(t);
        }

        if (types.Count == 0)
            throw new InvalidOperationException("No YgoDuelistCard types with YgoCardPackTags.Starter; add Starter to PackTags or the Neow grid cannot run.");

        sStarterCardTypes = types;
        sStarterBuckets = buckets;
        sAvailabilitySnapshot = buckets.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
        GD.Print($"[YgoDuelist NeowDraft] catalog built: {types.Count} starter types; bucket keys={buckets.Count}");
        MainFile.Logger.Info($"[NeowDraft catalog] structured buckets built starterTypeCount={types.Count}");
        LogStarterBucketDiagnostics(buckets);
    }

    /// <summary>
    /// Prints base <see cref="CardRarity"/> counts per <see cref="StarterCategory"/>, then full card lists for thin brackets:
    /// Uncommon if count &lt; <see cref="StarterDiagUncommonListBelow"/>, Common if count &lt; <see cref="StarterDiagCommonListBelow"/>.
    /// </summary>
    private static void LogStarterBucketDiagnostics(Dictionary<(StarterCategory, CardRarity), List<Type>> buckets)
    {
        static int CountIn(Dictionary<(StarterCategory, CardRarity), List<Type>> b, StarterCategory c, CardRarity r) =>
            b.TryGetValue((c, r), out List<Type>? l) ? l.Count : 0;

        GD.Print("[YgoDuelist StarterDiag] ========== starter-tagged pool: counts per category (Common / Uncommon / Rare) ==========");
        MainFile.Logger.Info("[StarterDiag] ========== starter-tagged pool: counts per category ==========");

        foreach (StarterCategory cat in Enum.GetValues<StarterCategory>())
        {
            int nCommon = CountIn(buckets, cat, CardRarity.Common);
            int nUncommon = CountIn(buckets, cat, CardRarity.Uncommon);
            int nRare = CountIn(buckets, cat, CardRarity.Rare);
            int sub = nCommon + nUncommon + nRare;
            string summary =
                $"[YgoDuelist StarterDiag] {cat}: Common={nCommon} Uncommon={nUncommon} Rare={nRare} (subtotal {sub})";
            GD.Print(summary);
            MainFile.Logger.Info(
                $"[StarterDiag] {cat}: Common={nCommon} Uncommon={nUncommon} Rare={nRare} (subtotal {sub})");

            if (nUncommon < StarterDiagUncommonListBelow)
                LogStarterBracketCardList(buckets, cat, CardRarity.Uncommon, nUncommon, StarterDiagUncommonListBelow);
            if (nCommon < StarterDiagCommonListBelow)
                LogStarterBracketCardList(buckets, cat, CardRarity.Common, nCommon, StarterDiagCommonListBelow);
        }

        GD.Print("[YgoDuelist StarterDiag] ========== end starter diagnostics ==========");
        MainFile.Logger.Info("[StarterDiag] ========== end starter diagnostics ==========");
    }

    private static void LogStarterBracketCardList(
        Dictionary<(StarterCategory, CardRarity), List<Type>> buckets,
        StarterCategory cat,
        CardRarity rarity,
        int actualCount,
        int threshold)
    {
        string header =
            $"[YgoDuelist StarterDiag] -- {cat} + {rarity}: count {actualCount} < {threshold}; listing all in this bracket --";
        GD.Print(header);
        MainFile.Logger.Info(
            $"[StarterDiag] -- {cat} + {rarity}: count {actualCount} < {threshold}; listing all in this bracket --");

        if (!buckets.TryGetValue((cat, rarity), out List<Type>? list) || list.Count == 0)
        {
            const string empty = "[YgoDuelist StarterDiag]   (empty bucket)";
            GD.Print(empty);
            MainFile.Logger.Info("[StarterDiag]   (empty bucket)");
            return;
        }

        foreach (Type t in list.OrderBy(x => CardFromType(x).Id.Entry, StringComparer.Ordinal))
        {
            CardModel m = CardFromType(t);
            string line = $"[YgoDuelist StarterDiag]   {m.Id.Entry} | {t.FullName}";
            GD.Print(line);
            MainFile.Logger.Info($"[StarterDiag]   {m.Id.Entry} | {t.FullName}");
        }
    }

    public static StarterCategory GetStarterCategory(CardModel model)
    {
        switch (model)
        {
            case BaseTrapCard:
                return StarterCategory.Trap;
            case BaseSpellCard:
                return StarterCategory.Spell;
            case BaseMonsterCard m:
                int lv = m.DuelMonsterLevel;
                if (lv <= 2)
                    return StarterCategory.Mon_Low;
                if (lv <= 4)
                    return StarterCategory.Mon_Mid;
                return StarterCategory.Mon_High;
            default:
                throw new InvalidOperationException(
                    $"Starter-tagged card {model.GetType().Name} must be {nameof(BaseSpellCard)}, {nameof(BaseTrapCard)}, or {nameof(BaseMonsterCard)}.");
        }
    }

    public static CardRarity NormalizeStarterRarity(CardRarity rarity) =>
        rarity switch
        {
            CardRarity.Basic => CardRarity.Common,
            CardRarity.Common => CardRarity.Common,
            CardRarity.Uncommon => CardRarity.Uncommon,
            CardRarity.Rare => CardRarity.Rare,
            _ => throw new InvalidOperationException($"Starter grid supports only Common/Uncommon/Rare (Basic maps to Common); got {rarity}.")
        };

    private static CardModel CardFromType(Type cardType)
    {
        sModelDbCardNoArg ??= typeof(ModelDb)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(ModelDb.Card) && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 0);

        MethodInfo closed = sModelDbCardNoArg.MakeGenericMethod(cardType);
        return (CardModel)closed.Invoke(null, null)!;
    }
}
