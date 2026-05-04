"""
Regenerate YgoMultiplayerSafeExplicitPicks.Generated.cs — types from STEMS plus corpse-blight helpers,
excluding types already covered at runtime by YgoMultiplayerSafePackRules (see that class in C#).

Authoritative for GetEffectivePackTags augmentation alongside C# rules. NOT the same script as
apply_multiplayer_safe_picks.py (that one mutates PackTags in card sources for a separate quota workflow).

NOT covered here (avoid repo-wide greps; add stems manually or put MultiplayerSafe on PackTags in the card):
  - “Simple” StatEffectTotal / StatEffectTotalMultiplier-only cards
  - Plain gain ATK / gain DEF only, hover-tip-only effects, destroyed-by-battle-only, “permanent scaling”
  - Tribute-this-monster-to-activate, Splinter-only / Blight-only — need case-by-case review
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS = ROOT / "YgoDuelistCode" / "Cards"
OUT = ROOT / "YgoDuelistCode" / "Services" / "YgoMultiplayerSafeExplicitPicks.Generated.cs"

# Stems and search patterns from the user list (file/class names in this repo use underscores).
STEMS: list[str] = [
    "Cost_Down",
    "Reload",
    "Legendary_Fiend",
    "Dark_Snake_Syndrome",
    "Narrow_Pass",
    "Stealth_Bird",
    "Timeater",
    "The_Fiend_Megacyber",
    "Great_Maju_Garzett",
    "Slate_Warrior",
    "Gray_Wing",
    "Dark_Magician_Girl",
    "Cyber_Jar",
    "Raigeki",
    "Penguin_Soldier",
    "Enraged_Muka_Muka",
    "Chaos_Necromancer",
    "Kuriboh",
    "Muka_Muka",
    "Big_Shield_Gardna",
    "Yomi_Ship",
    "Trap_Master",
    "Troop_Dragon",
    "Poison_Mummy",
    "Pumpking_the_King_of_Ghosts",
    "Little_Winguard",
    "Masked_Sorcerer",
    "Millennium_Shield",
    "Exarion_Universe",
    "Don_Turtle",
    "Gyaku_Gire_Panda",
    "Greenkappa",
    "Electric_Snake",
    "Dragon_Seeker",
    "Des_Koala",
    "Blast_Juggler",
    "Crass_Clown",
    "Soul_Resurrection",
    "Reinforcement_of_the_Army",
    "Reckless_Greed",
    "Graceful_Charity",
    "Cold_Wave",
    "Summoned_Skull",
    "Spirit_Ryu",
    "King_Tiger_Wanghu",
    "Machine_King",
    "Insect_Princess",
    "Insect_Soldiers_of_the_Sky",
    "Karate_Man",
    "Gora_Turtle",
    "Gilasaurus",
    "Gale_Lizard",
    "Freed_the_Brave_Wanderer",
    "Dream_Clown",
    "Drillago",
    "Dark_Elf",
    "Bubonic_Vermin",
    "Bowganian",
    "Cannon_Soldier",
    "Enraged_Battle_Ox",
    "Fear_from_the_Dark",
    "Zombyra_the_Dark",
    "Airknight_Parshath",
    "Winged_Minion",
    "The_Wicked_Worm_Beast",
    "Spear_Cretin",
    "Raigeki_Break",
    "Nightmare_Horse",
    "Needle_Ball",
    "Mystic_Lamp",
    "Hinotama",
    "Jinzo_7",
    "Iron_Blacksmith_Kotetsu",
    "Electric_Lizard",
    "Spirit_Reaper",
    "Queen_s_Double",
    "Outstanding_Dog_Marron",
    "Armed_Ninja",
    "Dark_Jeroid",
    "Kotodama",
    "D_D_Warrior_Lady",
    "D_D_Warrior",
    "Mudora",
    "Arcane_Archer_of_the_Forest",
    "Amazoness_Blowpiper",
    "Stumbling",
    "Riryoku",
    "Pyramid_Energy",
    "The_Law_of_the_Normal",
    "Nightmare_Wheel",
    "Hourglass_of_Courage",
    "Double_Spell",
    "Dragged_Down_into_the_Grave",
    "Final_Destiny",
    "The_Hunter_with_7_Weapons",
    "Thunder_Dragon",
    "Twin_Headed_Behemoth",
    "The_Kick_Man",
    "Tainted_Wisdom",
    "Maryokutai",
    "Lord_of_D",
    "Fairy_King_Truesdale",
    "Fairy_Guardian",
    "Exiled_Force",
    "Tribute_to_the_Doomed",
    "Secret_Barrel",
    "Piranha_Army",
    "Dark_Cat_with_White_Tail",
    "Dark_Mirror_Force",
    "Sparks",
    "Zone_Eater",
    "Skull_Servant",
    "Gigobyte",
    "Cat_s_Ear_Tribe",
    "Diffusion_Wave_Motion",
    "Spellbinding_Circle",
]


def ns_cls(path: Path) -> tuple[str, str]:
    ns = ""
    cls = ""
    text = path.read_text(encoding="utf-8")
    for line in text.splitlines():
        m = re.match(r"namespace\s+([\w.]+)", line)
        if m:
            ns = m.group(1)
        m = re.match(r"public\s+(?:sealed\s+)?class\s+(\w+)", line)
        if m:
            cls = m.group(1)
            break
    return ns, cls


def covered_by_narrow_rules(path: Path, full: str, cls: str, text: str) -> bool:
    s = str(path).replace("\\", "/")
    if "YgoCardPackTags.Draw" in text and "PackTags" in text:
        return True
    if ".Monster.Elemental." in full:
        return True
    if ".TrapMonster." in full:
        return True
    if "Gravekeeper" in cls:
        return True
    if "Monarch" in cls:
        return True
    if re.search(r"class\s+\w+\s*:\s*[^,\n]+,\s*IDoubleTributeMaterial", text):
        return True
    return False


def find_path_for_stem(stem: str) -> Path | None:
    exact = list(CARDS.rglob(f"{stem}.cs"))
    if len(exact) == 1:
        return exact[0]
    if len(exact) > 1:
        return exact[0]
    loose = [p for p in CARDS.rglob("*.cs") if p.stem.lower() == stem.lower()]
    if len(loose) == 1:
        return loose[0]
    return None


def corpse_blight_types() -> list[str]:
    """Execute-kill corpse blight helpers — matches user's 'Corpse Blight cards'."""
    needle = "ApplyHalfBlightToAllEnemiesOnExecuteKill"
    out: list[str] = []
    for p in CARDS.rglob("*.cs"):
        try:
            t = p.read_text(encoding="utf-8")
        except OSError:
            continue
        if needle not in t:
            continue
        ns, cls = ns_cls(p)
        if ns and cls:
            out.append(f"{ns}.{cls}")
    return sorted(set(out))


def main() -> None:
    full_names: set[str] = set()
    missing: list[str] = []

    for stem in STEMS:
        path = find_path_for_stem(stem)
        if path is None:
            missing.append(stem)
            continue
        text = path.read_text(encoding="utf-8")
        ns, cls = ns_cls(path)
        if not cls:
            missing.append(stem)
            continue
        full = f"{ns}.{cls}"
        if covered_by_narrow_rules(path, full, cls, text):
            continue
        full_names.add(full)

    for full in corpse_blight_types():
        full_names.add(full)

    lines = [
        "// <auto-generated />",
        "// python tools/generate_user_mp_safe_explicit.py",
        "",
        "using System.Collections.Generic;",
        "",
        "namespace YgoDuelist.YgoDuelistCode.Services;",
        "",
        "/// <summary>Explicit multiplayer-safe picks (see tools/generate_user_mp_safe_explicit.py). Draw / Elemental / TrapMonster / Gravekeeper / Monarch / IDoubleTributeMaterial / true normal PackWeightMultiplier &gt;= 1.1 are handled in YgoMultiplayerSafePackRules instead.</summary>",
        "internal static class YgoMultiplayerSafeExplicitPicks",
        "{",
        "    internal static readonly HashSet<string> FullNames = new HashSet<string>(System.StringComparer.Ordinal)",
        "    {",
    ]
    for n in sorted(full_names):
        lines.append(f'        "{n}",')
    lines.extend(["    };", "}"])

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {len(full_names)} explicit picks to {OUT}")
    if missing:
        print("Unresolved stems (fix name or add file):", ", ".join(missing))


if __name__ == "__main__":
    main()
