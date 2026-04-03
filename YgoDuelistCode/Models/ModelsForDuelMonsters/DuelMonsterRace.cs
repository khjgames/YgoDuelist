namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Duel monster type / race (YGO). Icons: <c>YgoDuelist/images/card_frames/Race/</c>.
/// Numeric values are stable (keyword / save mapping). The compendium race filters list every value with readable labels in <see cref="YgoDuelist.YgoDuelistCode.Nodes.CardLibrary.YgoCardLibraryRaceSidebarLabels"/>.
/// </summary>
public enum DuelMonsterRace
{
    Aqua,
    Beast,
    BeastWarrior,
    Dinosaur,
    DivineBeast,
    Dragon,
    Fairy,
    Fiend,
    Fish,
    Insect,
    Machine,
    Plant,
    Psychic,
    Pyro,
    Reptile,
    Rock,
    SeaSerpent,
    Spellcaster,
    Thunder,
    Warrior,
    WingedBeast,
    Wyrm,
    Zombie,

    /// <summary>Spell Card (race Normal in card data).</summary>
    SpellNormal,
    /// <summary>Spell Card — Continuous.</summary>
    SpellContinuous,
    /// <summary>Spell Card — Quick-Play.</summary>
    SpellQuickPlay,
    /// <summary>Spell Card — Equip.</summary>
    SpellEquip,
    /// <summary>Spell Card — Field.</summary>
    SpellField,
    /// <summary>Spell Card — Ritual.</summary>
    SpellRitual,

    /// <summary>Trap Card (race Normal in card data).</summary>
    TrapNormal,
    /// <summary>Trap Card — Continuous.</summary>
    TrapContinuous,
    /// <summary>Trap Card — Counter.</summary>
    TrapCounter,
}
