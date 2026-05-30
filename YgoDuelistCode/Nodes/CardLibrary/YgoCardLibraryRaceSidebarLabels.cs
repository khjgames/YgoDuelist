using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// Compendium race/type filter: one row per <see cref="DuelMonsterRace"/>, with readable tickbox text
/// (not raw <c>ToString()</c>) and stable sidebar order.
/// </summary>
internal static class YgoCardLibraryRaceSidebarLabels
{
    /// <summary>Every race exactly once, declaration order in <see cref="DuelMonsterRace"/>.</summary>
    internal static readonly DuelMonsterRace[] SidebarOrder =
    [
        DuelMonsterRace.Aqua,
        DuelMonsterRace.Beast,
        DuelMonsterRace.BeastWarrior,
        DuelMonsterRace.Dinosaur,
        DuelMonsterRace.DivineBeast,
        DuelMonsterRace.Dragon,
        DuelMonsterRace.Fairy,
        DuelMonsterRace.Fiend,
        DuelMonsterRace.Fish,
        DuelMonsterRace.Insect,
        DuelMonsterRace.Machine,
        DuelMonsterRace.Plant,
        DuelMonsterRace.Psychic,
        DuelMonsterRace.Pyro,
        DuelMonsterRace.Reptile,
        DuelMonsterRace.Rock,
        DuelMonsterRace.SeaSerpent,
        DuelMonsterRace.Spellcaster,
        DuelMonsterRace.Thunder,
        DuelMonsterRace.Warrior,
        DuelMonsterRace.WingedBeast,
        DuelMonsterRace.Wyrm,
        DuelMonsterRace.Zombie,
        DuelMonsterRace.SpellNormal,
        DuelMonsterRace.SpellContinuous,
        DuelMonsterRace.SpellQuickPlay,
        DuelMonsterRace.SpellEquip,
        DuelMonsterRace.SpellField,
        DuelMonsterRace.SpellRitual,
        DuelMonsterRace.TrapNormal,
        DuelMonsterRace.TrapContinuous,
        DuelMonsterRace.TrapCounter,
    ];

    /// <summary>Short label on the cost-style library tickbox; hover uses <c>RACE_FILTER_{enum}</c>.</summary>
    internal static string TickboxLabel(DuelMonsterRace race) =>
        race switch
        {
            DuelMonsterRace.Aqua => "Aqua",
            DuelMonsterRace.Beast => "Beast",
            DuelMonsterRace.BeastWarrior => "Beast-Warrior",
            DuelMonsterRace.Dinosaur => "Dinosaur",
            DuelMonsterRace.DivineBeast => "Divine-Beast",
            DuelMonsterRace.Dragon => "Dragon",
            DuelMonsterRace.Fairy => "Fairy",
            DuelMonsterRace.Fiend => "Fiend",
            DuelMonsterRace.Fish => "Fish",
            DuelMonsterRace.Insect => "Insect",
            DuelMonsterRace.Machine => "Machine",
            DuelMonsterRace.Plant => "Plant",
            DuelMonsterRace.Psychic => "Psychic",
            DuelMonsterRace.Pyro => "Pyro",
            DuelMonsterRace.Reptile => "Reptile",
            DuelMonsterRace.Rock => "Rock",
            DuelMonsterRace.SeaSerpent => "Sea Serpent",
            DuelMonsterRace.Spellcaster => "Spellcaster",
            DuelMonsterRace.Thunder => "Thunder",
            DuelMonsterRace.Warrior => "Warrior",
            DuelMonsterRace.WingedBeast => "Winged Beast",
            DuelMonsterRace.Wyrm => "Wyrm",
            DuelMonsterRace.Zombie => "Zombie",
            DuelMonsterRace.SpellNormal => "Spell (Normal)",
            DuelMonsterRace.SpellContinuous => "Spell (Cont.)",
            DuelMonsterRace.SpellQuickPlay => "Spell (Quick)",
            DuelMonsterRace.SpellEquip => "Spell (Equip)",
            DuelMonsterRace.SpellField => "Spell (Field)",
            DuelMonsterRace.SpellRitual => "Spell (Ritual)",
            DuelMonsterRace.TrapNormal => "Trap (Normal)",
            DuelMonsterRace.TrapContinuous => "Trap (Cont.)",
            DuelMonsterRace.TrapCounter => "Trap (Counter)",
            _ => race.ToString(),
        };
}
