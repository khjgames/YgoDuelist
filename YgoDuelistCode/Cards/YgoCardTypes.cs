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
    Starter = 1L << 28
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
