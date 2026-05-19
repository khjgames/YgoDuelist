using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// ZGO card type for frame/background styling (spell, trap, monster, etc.).
/// </summary>
public enum YgoCardType
{
    Spell,
    Trap,
    Monster,
    EffectMonster,
    FusionMonster,
    RitualMonster,
}

[Flags]
public enum YgoCardPackTags : long // up to 64 flags
{
    None = 0,
    Earth = 1L << 1,
    Water = 1L << 2,
    Wind = 1L << 3,
    Fire = 1L << 4,
    Dark = 1L << 5,
    Light = 1L << 6,
    Fusion = 1L << 7,
    Ritual = 1L << 8,
    Ocean = 1L << 9,
    Insect = 1L << 10,
    Machine = 1L << 11,
    Dragon = 1L << 12,
    Zombie = 1L << 13,
    Fiend = 1L << 14,
    Spellcaster = 1L << 15,
    Warrior = 1L << 16,
    Heal = 1L << 17,
    Draw = 1L << 18,
    Chance = 1L << 19,
    Burn = 1L << 20,
    Normal = 1L << 21,
    Spell = 1L << 22,
    Trap = 1L << 23,
    Banish = 1L << 24,
    WinCon = 1L << 25,
    God = 1L << 26,
    Bundled = 1L << 27,
    Starter = 1L << 28,
    MultiplayerSafe = 1L << 29
}

/// <summary>
/// Pack-related groupings for <see cref="YgoDuelistCard.GetRelatedCards"/> and shared related-card pools.
/// Combine with <c>|</c> when a card belongs to multiple groups.
/// </summary>
[Flags]
public enum YgoCardArchetype : ulong
{
    None = 0,
    /// <summary>Zombie boost package (field/equip synergy).</summary>
    ZombieBoost = 1UL << 0,
    /// <summary>Blue-Eyes White Dragon archetype flag: <see cref="YgoCardArchetypeRegistry.GetTypes"/> = related pool; <see cref="YgoCardArchetypeRegistry.GetStrictArchetypeTypes"/> = strict in-archetype for rules/keywords.</summary>
    BlueEyesWhiteDragon = 1UL << 1,
    /// <summary>Dark Magician archetype flag: related vs strict split same as <see cref="BlueEyesWhiteDragon"/>.</summary>
    DarkMagician = 1UL << 2,
    /// <summary>Monsters that count as two tributes for eligible normal summons.</summary>
    GenericDoubleSummoner = 1UL << 3,

    /// <summary>Earth attribute boost (Gaia Power, Milus Radiant, Forest).</summary>
    EarthBoost = 1UL << 4,
    /// <summary>Water attribute boost (Umiiruka, Star Boy, Umi, A Legendary Ocean).</summary>
    WaterBoost = 1UL << 5,
    /// <summary>Attribute field + elemental ally (Wind).</summary>
    WindBoost = 1UL << 6,
    /// <summary>Attribute field + elemental ally (Fire).</summary>
    FireBoost = 1UL << 7,
    /// <summary>Attribute field + elemental ally (Dark).</summary>
    DarkBoost = 1UL << 8,
    /// <summary>Attribute field + elemental ally (Light).</summary>
    LightBoost = 1UL << 9,

    /// <summary>Flat race equip + matching terrain (aligned with starter flat race equips and terrain fields).</summary>
    AquaBoost = 1UL << 10,
    BeastBoost = 1UL << 11,
    BeastWarriorBoost = 1UL << 12,
    DinosaurBoost = 1UL << 13,
    DragonBoost = 1UL << 14,
    FairyBoost = 1UL << 15,
    FiendBoost = 1UL << 16,
    InsectBoost = 1UL << 17,
    MachineBoost = 1UL << 18,
    PlantBoost = 1UL << 19,
    SpellcasterBoost = 1UL << 20,
    ThunderBoost = 1UL << 21,
    WarriorBoost = 1UL << 22,
    WingedBeastBoost = 1UL << 23,
    /// <summary>Wasteland terrain (no flat Rock equip in pool).</summary>
    RockBoost = 1UL << 24,

    /// <summary>Cards that place or consume Spell Counters.</summary>
    SpellCounter = 1UL << 25,
    /// <summary>Red-Eyes Black Dragon family and fusion materials.</summary>
    RedEyesBlackDragon = 1UL << 26,
    /// <summary>Harpie Lady family and support.</summary>
    HarpieLady = 1UL << 27,
    /// <summary>God pack (<see cref="YgoCardPackTags.God"/>) cards; pool built from pack tags.</summary>
    DivineBeast = 1UL << 28,
    /// <summary>Heads/Tails coin flip resolution.</summary>
    Coinflip = 1UL << 29,
    /// <summary>Dice rolls (d6, Graceful Dice, etc.).</summary>
    Diceroll = 1UL << 30,
    /// <summary>Attacks or effects that apply Blight damage.</summary>
    Blight = 1UL << 31,
    /// <summary>Attacks or equips that apply Splinter damage.</summary>
    Splinter = 1UL << 32,
    /// <summary>Take blockable damage to the player (costs, upkeep, wrong coin calls).</summary>
    TakeDamage = 1UL << 33,
    /// <summary>Combat-end Doom / RaDoomed-style downside packages.</summary>
    Doomed = 1UL << 34,
    /// <summary>Monsters with permanent or scaling printed ATK/DEF growth.</summary>
    GrowthType = 1UL << 35,
    /// <summary>Necrovalley and Gravekeeper monsters.</summary>
    Gravekeeper = 1UL << 36,
    /// <summary>Umi, A Legendary Ocean, and direct sea-field synergy.</summary>
    Umi = 1UL << 37,
    /// <summary>Heal when a monster is summoned (including self on summon).</summary>
    SummonHeal = 1UL << 38,
    /// <summary>Heal the player without summon-gated triggers.</summary>
    SelfHeal = 1UL << 39,
    /// <summary>Spells and same-turn monster effects that grant Block.</summary>
    QuickBlock = 1UL << 40,
    /// <summary>Traps that grant Block on activation.</summary>
    SlowBlock = 1UL << 41,
    /// <summary>All-monster temporary ATK/DEF boost until end of turn.</summary>
    GenericAllMonstersTempStatBoost = 1UL << 42,
    /// <summary>Continuous field-wide or team-wide ATK/DEF modifiers.</summary>
    GenericAllMonstersContinuousStatBoost = 1UL << 43,
    /// <summary>Single-target temporary ATK/DEF shift.</summary>
    GenericSingleMonsterTempStatBoost = 1UL << 44,
    /// <summary>Energy discounts, surcharges, or summon cost modifiers.</summary>
    Energy = 1UL << 45,
    /// <summary>Spirit Monster family (return to hand at turn end while face-up).</summary>
    SpiritMonster = 1UL << 46,
}

/// <summary>How a single fusion material slot accepts materials: exact named card, requirement filter only, or either.</summary>
public enum FusionMaterialSlotMode : byte
{
    /// <summary>Exact <see cref="FusionMaterialSlot.NamedType"/> match, or one global fusion substitute.</summary>
    NamedOnly,
    /// <summary>Monster must satisfy <see cref="FusionMaterialSlot.Req"/>; substitutes never apply.</summary>
    RequirementOnly,
    /// <summary>Named type or substitute, or requirement match (substitute does not satisfy the requirement path).</summary>
    NamedOrRequirement
}

/// <summary>Which stat filters are active on a <see cref="FusionMaterialRequirements"/> instance.</summary>
[Flags]
public enum FusionMaterialRequirementFilterMask : byte
{
    None = 0,
    Level = 1 << 0,
    Attribute = 1 << 1,
    Atk = 1 << 2,
    Def = 1 << 3,
    Race = 1 << 4
}

/// <summary>
/// Implement on card models that use ZGO-style card frames/backgrounds.
/// </summary>
public interface IYgoCard
{
    YgoCardType YgoCardType { get; }

    /// <summary>Monster type / spell subtype / trap subtype for YGO frame data (from card JSON <c>race</c>).</summary>
    DuelMonsterRace DuelMonsterRace { get; }
}
