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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;

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
/// If the grid contains a <see cref="RitualSpellCard"/>, its ritual target monster is inserted immediately after that spell (not counted toward category quotas).
/// Otherwise, if the grid contains a named <see cref="RitualMonsterCard"/> with a paired spell in <see cref="RitualArchetypeMeta"/>, that spell is inserted immediately after the monster.
/// At most one such bonus row runs so the list stays at <see cref="MaxGridSize"/> when a bonus applies.
/// After that, signature spells may replace a level 5–6 monster with <see cref="Dark_Magician"/> or <see cref="Blue_Eyes_White_Dragon"/>, then <see cref="Necrovalley"/> may add <see cref="A_Cat_of_Ill_Omen"/> (level 1–2), <see cref="Gravekeeper_s_Curse"/>, and <see cref="Gravekeeper_s_Spear_Soldier"/> (see <see cref="ApplyNeowSignatureMonsterSubstitutions"/>).
/// Then one random flat race equip (if any), one random terrain race field (if any), and one random flat attribute field (if any)
/// may be retargeted to match the dominant monster races/attributes in the grid.
/// </summary>
public static class YgoStarterCardCatalog
{
    /// <summary>Structured Neow draft slots (before optional ritual bundled monster).</summary>
    public const int GridSize = 19;

    /// <summary>Maximum cards shown when a paired ritual bonus row is added: spell→monster or monster→spell (<see cref="GridSize"/> + 1).</summary>
    public const int MaxGridSize = 20;

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

    /// <summary>Curated <see cref="FlatRaceEquipSpell"/> types keyed by required race (see <c>FlatRaceEquipSpells.cs</c>).</summary>
    private static readonly IReadOnlyDictionary<DuelMonsterRace, Type> FlatRaceEquipTypeByRace =
        new Dictionary<DuelMonsterRace, Type>
        {
            [DuelMonsterRace.Aqua] = typeof(Power_Of_Kaishin),
            [DuelMonsterRace.Beast] = typeof(Beast_Fangs),
            [DuelMonsterRace.BeastWarrior] = typeof(Mystical_Moon),
            [DuelMonsterRace.Dinosaur] = typeof(Raise_Body_Heat),
            [DuelMonsterRace.Dragon] = typeof(Dragon_Treasure),
            [DuelMonsterRace.Fairy] = typeof(Silver_Bow_And_Arrow),
            [DuelMonsterRace.Fiend] = typeof(Dark_Energy),
            [DuelMonsterRace.Insect] = typeof(Laser_Cannon_Armor),
            [DuelMonsterRace.Machine] = typeof(Machine_Conversion_Factory),
            [DuelMonsterRace.Plant] = typeof(Vile_Germs),
            [DuelMonsterRace.Spellcaster] = typeof(Book_Of_Secret_Arts),
            [DuelMonsterRace.Thunder] = typeof(Electro_Whip),
            [DuelMonsterRace.Warrior] = typeof(Legendary_Sword),
            [DuelMonsterRace.WingedBeast] = typeof(Follow_Wind),
            [DuelMonsterRace.Zombie] = typeof(Violet_Crystal),
        };

    /// <summary>Flat terrain fields in the Neow pool: positive <see cref="BaseFieldSpellCard.GetFieldStatEffect"/> races match in-game cards.</summary>
    private static readonly (Type FieldType, Func<DuelMonsterRace, bool> IsBuffed)[] TerrainRaceFieldSpecs =
    [
        (typeof(Forest), static r => r is DuelMonsterRace.Insect or DuelMonsterRace.Beast or DuelMonsterRace.Plant or DuelMonsterRace.BeastWarrior),
        (typeof(Mountain), static r => r is DuelMonsterRace.Dragon or DuelMonsterRace.WingedBeast or DuelMonsterRace.Thunder),
        (typeof(Sogen), static r => r is DuelMonsterRace.Warrior or DuelMonsterRace.BeastWarrior),
        (typeof(Wasteland), static r => r is DuelMonsterRace.Dinosaur or DuelMonsterRace.Zombie or DuelMonsterRace.Rock),
        (typeof(Yami), static r => r is DuelMonsterRace.Fiend or DuelMonsterRace.Spellcaster),
        (typeof(Umi), static r => r is DuelMonsterRace.Fish or DuelMonsterRace.SeaSerpent or DuelMonsterRace.Thunder or DuelMonsterRace.Aqua),
    ];

    private static readonly IReadOnlyDictionary<DuelMonsterAttribute, Type> FlatAttributeFieldTypeByAttribute =
        new Dictionary<DuelMonsterAttribute, Type>
        {
            [DuelMonsterAttribute.Earth] = typeof(Gaia_Power),
            [DuelMonsterAttribute.Light] = typeof(Luminous_Spark),
            [DuelMonsterAttribute.Dark] = typeof(Mystic_Plasma_Zone),
            [DuelMonsterAttribute.Fire] = typeof(Molten_Destruction),
            [DuelMonsterAttribute.Wind] = typeof(Rising_Air_Current),
            [DuelMonsterAttribute.Water] = typeof(Umiiruka),
        };

    private static readonly HashSet<Type> FlatAttributeFieldTypes = new()
    {
        typeof(Gaia_Power),
        typeof(Luminous_Spark),
        typeof(Mystic_Plasma_Zone),
        typeof(Molten_Destruction),
        typeof(Rising_Air_Current),
        typeof(Umiiruka),
    };

    static YgoStarterCardCatalog()
    {
        int sum = CategorySlotTotals.Values.Sum();
        if (sum != GridSize)
            throw new InvalidOperationException($"YgoStarterCardCatalog: category slot sum {sum} != {nameof(GridSize)} {GridSize}.");
        if (StarterCardRarities.Length != GridSize)
            throw new InvalidOperationException($"{nameof(StarterCardRarities)}.Length must equal {nameof(GridSize)}.");
        if (MaxGridSize != GridSize + 1)
            throw new InvalidOperationException($"{nameof(MaxGridSize)} must be {nameof(GridSize)} + 1.");
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

        bool spellBundledMonster = TryInsertBundledRitualMonsterAfterFirstSpell(grid);
        bool monsterBundledSpell = !spellBundledMonster && TryInsertBundledRitualSpellAfterFirstMonster(grid);
        bool ritualBundled = spellBundledMonster || monsterBundledSpell;

        ApplyNeowSignatureMonsterSubstitutions(grid, rng);
        ApplyNeowStarterGridSubstitutions(grid, rng);

        GD.Print(
            $"[YgoDuelist NeowDraft] CreateRandomGrid: structured fill categoryOrder=[{string.Join(",", categoryOrder)}] " +
            $"finalCount={grid.Count} ritualBundledBonus={ritualBundled} spellBundledMonster={spellBundledMonster} monsterBundledSpell={monsterBundledSpell} " +
            $"distinctIds={grid.Select(c => c.Id.Entry).Distinct().Count()}");
        MainFile.Logger.Info(
            $"[NeowDraft catalog] structured grid built finalCount={grid.Count} ritualBundledBonus={ritualBundled} spellBundledMonster={spellBundledMonster} monsterBundledSpell={monsterBundledSpell} distinctIds={grid.Select(c => c.Id.Entry).Distinct().Count()}");
        return grid;
    }

    /// <summary>
    /// Inserts the paired <see cref="RitualSpellCard"/> from <see cref="RitualArchetypeMeta.PairedRitualSpellType"/> immediately after the first such <see cref="RitualMonsterCard"/> in <paramref name="grid"/>,
    /// only if that spell is not already present. Does not use starter buckets or category quotas.
    /// </summary>
    /// <returns>True if one bonus card was inserted (grid size becomes <see cref="MaxGridSize"/>).</returns>
    private static bool TryInsertBundledRitualSpellAfterFirstMonster(List<CardModel> grid)
    {
        if (grid.Count >= MaxGridSize)
            return false;

        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] is not RitualMonsterCard monster)
                continue;

            Type? spellType = RitualArchetypeMeta.PairedRitualSpellType(monster.GetType());
            if (spellType == null || spellType.IsAbstract || !typeof(RitualSpellCard).IsAssignableFrom(spellType))
                continue;

            CardModel bundledCanonical;
            try
            {
                bundledCanonical = CardFromType(spellType);
            }
            catch
            {
                continue;
            }

            string entry = bundledCanonical.Id.Entry;
            if (grid.Any(c => c.Id.Entry == entry))
                continue;

            CardModel bundled = bundledCanonical.ToMutable();
            bundled.FloorAddedToDeck = 1;
            grid.Insert(i + 1, bundled);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Inserts the <see cref="RitualSpellCard.RitualTargetMonsterType"/> model immediately after the first ritual spell in <paramref name="grid"/>,
    /// only if that monster is not already present. Does not use starter buckets or category quotas.
    /// </summary>
    /// <returns>True if one bonus card was inserted (grid size becomes <see cref="MaxGridSize"/>).</returns>
    private static bool TryInsertBundledRitualMonsterAfterFirstSpell(List<CardModel> grid)
    {
        if (grid.Count >= MaxGridSize)
            return false;

        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] is not RitualSpellCard ritual)
                continue;

            Type mt = ritual.RitualTargetMonsterType;
            if (mt.IsAbstract || !typeof(RitualMonsterCard).IsAssignableFrom(mt))
                continue;

            CardModel bundledCanonical;
            try
            {
                bundledCanonical = CardFromType(mt);
            }
            catch
            {
                continue;
            }

            string entry = bundledCanonical.Id.Entry;
            if (grid.Any(c => c.Id.Entry == entry))
                continue;

            CardModel bundled = bundledCanonical.ToMutable();
            bundled.FloorAddedToDeck = 1;
            grid.Insert(i + 1, bundled);
            return true;
        }

        return false;
    }

    /// <summary>
    /// If the grid includes Dark Magic support spells or Burst Stream, swaps the lowest-rarity level 5–6 monster for
    /// <see cref="Dark_Magician"/> or <see cref="Blue_Eyes_White_Dragon"/> respectively (Burst Stream runs after Dark Magic).
    /// Skips if the replacement is already present or no level 5–6 monster exists.
    /// After Burst Stream, <see cref="Necrovalley"/> may replace the lowest-rarity level 1–2 monster with <see cref="A_Cat_of_Ill_Omen"/> if absent,
    /// then up to two lowest-rarity level 3–4 monsters with <see cref="Gravekeeper_s_Curse"/> and <see cref="Gravekeeper_s_Spear_Soldier"/> for any that are not already in the grid.
    /// </summary>
    private static void ApplyNeowSignatureMonsterSubstitutions(List<CardModel> grid, Rng rng)
    {
        bool hasDarkMagicSpell = grid.Any(c => c is Dark_Magic_Attack or Diffusion_Wave_Motion);
        if (hasDarkMagicSpell)
            TryReplaceLowestRarityLevel56MonsterWith(grid, rng, typeof(Dark_Magician));

        bool hasBurstStream = grid.Any(c => c is Burst_Stream_of_Destruction);
        if (hasBurstStream)
            TryReplaceLowestRarityLevel56MonsterWith(grid, rng, typeof(Blue_Eyes_White_Dragon));

        TryNecrovalleyGravekeeperSubstitutions(grid, rng);
    }

    private static void TryNecrovalleyGravekeeperSubstitutions(List<CardModel> grid, Rng rng)
    {
        if (!grid.Any(c => c is Necrovalley))
            return;

        TryReplaceLowestRarityLevel12MonsterWith(grid, rng, typeof(A_Cat_of_Ill_Omen));

        Type curseType = typeof(Gravekeeper_s_Curse);
        Type spearType = typeof(Gravekeeper_s_Spear_Soldier);

        bool needCurse = !StarterGridContainsCardType(grid, curseType);
        bool needSpear = !StarterGridContainsCardType(grid, spearType);

        if (!needCurse && !needSpear)
            return;

        var replacements = new List<Type>();
        if (needCurse)
            replacements.Add(curseType);
        if (needSpear)
            replacements.Add(spearType);

        var candidateIndices = new List<int>();
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] is not BaseMonsterCard m)
                continue;
            if (m.DuelMonsterLevel is not (3 or 4))
                continue;
            candidateIndices.Add(i);
        }

        if (candidateIndices.Count == 0)
            return;

        List<int> slots = PickLowestRarityDistinctMonsterSlots(grid, candidateIndices, rng, replacements.Count);
        if (slots.Count < replacements.Count)
            return;

        for (int i = 0; i < replacements.Count; i++)
            ReplaceStarterGridSlot(grid, slots[i], replacements[i]);
    }

    /// <summary>
    /// Picks <paramref name="count"/> distinct indices from <paramref name="candidateIndices"/> by repeatedly choosing
    /// a random slot among those with the lowest <see cref="StarterRarityRank"/> in the remaining pool.
    /// </summary>
    private static List<int> PickLowestRarityDistinctMonsterSlots(
        List<CardModel> grid,
        List<int> candidateIndices,
        Rng rng,
        int count)
    {
        var pool = new List<int>(candidateIndices);
        var result = new List<int>(count);
        for (int n = 0; n < count; n++)
        {
            if (pool.Count == 0)
                break;

            int minRank = pool.Min(i => StarterRarityRank(grid[i]));
            List<int> tied = pool.Where(i => StarterRarityRank(grid[i]) == minRank).ToList();
            int pick = tied[rng.NextInt(0, tied.Count)];
            result.Add(pick);
            pool.Remove(pick);
        }

        return result;
    }

    private static void TryReplaceLowestRarityLevel56MonsterWith(List<CardModel> grid, Rng rng, Type replacementMonsterType)
    {
        if (StarterGridContainsCardType(grid, replacementMonsterType))
            return;

        var candidateIndices = new List<int>();
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] is not BaseMonsterCard m)
                continue;
            int lv = m.DuelMonsterLevel;
            if (lv is not (5 or 6))
                continue;
            candidateIndices.Add(i);
        }

        if (candidateIndices.Count == 0)
            return;

        int minRank = candidateIndices.Min(i => StarterRarityRank(grid[i]));
        List<int> tied = candidateIndices.Where(i => StarterRarityRank(grid[i]) == minRank).ToList();
        int pick = tied[rng.NextInt(0, tied.Count)];
        ReplaceStarterGridSlot(grid, pick, replacementMonsterType);
    }

    private static void TryReplaceLowestRarityLevel12MonsterWith(List<CardModel> grid, Rng rng, Type replacementMonsterType)
    {
        if (StarterGridContainsCardType(grid, replacementMonsterType))
            return;

        var candidateIndices = new List<int>();
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] is not BaseMonsterCard m)
                continue;
            if (m.DuelMonsterLevel is not (1 or 2))
                continue;
            candidateIndices.Add(i);
        }

        if (candidateIndices.Count == 0)
            return;

        int minRank = candidateIndices.Min(i => StarterRarityRank(grid[i]));
        List<int> tied = candidateIndices.Where(i => StarterRarityRank(grid[i]) == minRank).ToList();
        int pick = tied[rng.NextInt(0, tied.Count)];
        ReplaceStarterGridSlot(grid, pick, replacementMonsterType);
    }

    private static bool StarterGridContainsCardType(List<CardModel> grid, Type cardType)
    {
        string entry = CardFromType(cardType).Id.Entry;
        return grid.Any(c => c.Id.Entry == entry);
    }

    private static int StarterRarityRank(CardModel model)
    {
        return NormalizeStarterRarity(model.Rarity) switch
        {
            CardRarity.Common => 0,
            CardRarity.Uncommon => 1,
            CardRarity.Rare => 2,
            _ => 99
        };
    }

    /// <summary>
    /// After ritual bundle rows, optionally retargets one flat race equip, one terrain race field, and one flat attribute field
    /// toward the most common monster races/attributes in the grid (ties broken at random).
    /// Chooses a replacement that is not already elsewhere in the grid; if the best match would duplicate, uses the next tier down.
    /// </summary>
    private static void ApplyNeowStarterGridSubstitutions(List<CardModel> grid, Rng rng)
    {
        var monsters = new List<BaseMonsterCard>();
        foreach (CardModel c in grid)
        {
            if (c is BaseMonsterCard m)
                monsters.Add(m);
        }

        if (monsters.Count == 0)
            return;

        TrySubstituteFlatRaceEquip(grid, rng, monsters);
        TrySubstituteTerrainRaceField(grid, rng, monsters);
        TrySubstituteFlatAttributeField(grid, rng, monsters);
    }

    private static void TrySubstituteFlatRaceEquip(List<CardModel> grid, Rng rng, List<BaseMonsterCard> monsters)
    {
        var equipIndices = new List<int>();
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] is FlatRaceEquipSpell)
                equipIndices.Add(i);
        }

        if (equipIndices.Count == 0)
            return;

        var raceCounts = new Dictionary<DuelMonsterRace, int>();
        foreach (BaseMonsterCard m in monsters)
        {
            DuelMonsterRace r = m.DuelMonsterRace;
            raceCounts[r] = raceCounts.GetValueOrDefault(r) + 1;
        }

        int slot = equipIndices[rng.NextInt(0, equipIndices.Count)];

        var tiers = raceCounts
            .Where(kv => FlatRaceEquipTypeByRace.ContainsKey(kv.Key))
            .GroupBy(kv => kv.Value)
            .OrderByDescending(g => g.Key);

        foreach (var tier in tiers)
        {
            List<DuelMonsterRace> racesInTier = tier.Select(kv => kv.Key).ToList();
            racesInTier.UnstableShuffle(rng);
            foreach (DuelMonsterRace race in racesInTier)
            {
                Type equipType = FlatRaceEquipTypeByRace[race];
                if (!IsCardEntryPresentElsewhere(grid, equipType, slot))
                {
                    ReplaceStarterGridSlot(grid, slot, equipType);
                    return;
                }
            }
        }
    }

    private static void TrySubstituteTerrainRaceField(List<CardModel> grid, Rng rng, List<BaseMonsterCard> monsters)
    {
        var terrainIndices = new List<int>();
        for (int i = 0; i < grid.Count; i++)
        {
            Type t = grid[i].GetType();
            foreach ((Type fieldType, _) in TerrainRaceFieldSpecs)
            {
                if (fieldType == t)
                {
                    terrainIndices.Add(i);
                    break;
                }
            }
        }

        if (terrainIndices.Count == 0)
            return;

        var scores = new List<(Type FieldType, int Count)>();
        foreach ((Type fieldType, Func<DuelMonsterRace, bool> isBuffed) in TerrainRaceFieldSpecs)
        {
            int n = monsters.Count(m => isBuffed(m.DuelMonsterRace));
            scores.Add((fieldType, n));
        }

        int slot = terrainIndices[rng.NextInt(0, terrainIndices.Count)];

        foreach (IGrouping<int, (Type FieldType, int Count)> tier in scores.GroupBy(s => s.Count).OrderByDescending(g => g.Key))
        {
            List<Type> fieldsInTier = tier.Select(s => s.FieldType).ToList();
            fieldsInTier.UnstableShuffle(rng);
            foreach (Type fieldType in fieldsInTier)
            {
                if (!IsCardEntryPresentElsewhere(grid, fieldType, slot))
                {
                    ReplaceStarterGridSlot(grid, slot, fieldType);
                    return;
                }
            }
        }
    }

    private static void TrySubstituteFlatAttributeField(List<CardModel> grid, Rng rng, List<BaseMonsterCard> monsters)
    {
        var attrIndices = new List<int>();
        for (int i = 0; i < grid.Count; i++)
        {
            Type t = grid[i].GetType();
            if (FlatAttributeFieldTypes.Contains(t))
                attrIndices.Add(i);
        }

        if (attrIndices.Count == 0)
            return;

        var attrCounts = new Dictionary<DuelMonsterAttribute, int>();
        foreach (BaseMonsterCard m in monsters)
        {
            DuelMonsterAttribute a = m.DuelMonsterAttribute;
            attrCounts[a] = attrCounts.GetValueOrDefault(a) + 1;
        }

        int slot = attrIndices[rng.NextInt(0, attrIndices.Count)];

        foreach (IGrouping<int, KeyValuePair<DuelMonsterAttribute, int>> tier in attrCounts
                     .GroupBy(kv => kv.Value)
                     .OrderByDescending(g => g.Key))
        {
            List<DuelMonsterAttribute> attrsInTier = tier
                .Select(kv => kv.Key)
                .Where(a => FlatAttributeFieldTypeByAttribute.ContainsKey(a))
                .ToList();
            if (attrsInTier.Count == 0)
                continue;

            attrsInTier.UnstableShuffle(rng);
            foreach (DuelMonsterAttribute attr in attrsInTier)
            {
                Type fieldType = FlatAttributeFieldTypeByAttribute[attr];
                if (!IsCardEntryPresentElsewhere(grid, fieldType, slot))
                {
                    ReplaceStarterGridSlot(grid, slot, fieldType);
                    return;
                }
            }
        }
    }

    /// <returns>True if <paramref name="cardType"/>'s id appears on any grid card other than <paramref name="ignoreIndex"/>.</returns>
    private static bool IsCardEntryPresentElsewhere(List<CardModel> grid, Type cardType, int ignoreIndex)
    {
        string entry = CardFromType(cardType).Id.Entry;
        for (int i = 0; i < grid.Count; i++)
        {
            if (i == ignoreIndex)
                continue;
            if (grid[i].Id.Entry == entry)
                return true;
        }

        return false;
    }

    private static void ReplaceStarterGridSlot(List<CardModel> grid, int index, Type cardType)
    {
        CardModel canonical = CardFromType(cardType);
        CardModel mutable = canonical.ToMutable();
        mutable.FloorAddedToDeck = 1;
        grid[index] = mutable;
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
