"""
One-off: apply Planned_Starter_Cards.md pack tags, RelatedCards (self), commented BundledCards,
and optional rarity from lines 1-178. Skips example cards the user marked done.
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS = ROOT / "YgoDuelistCode" / "Cards"
PLANNED = ROOT / "Game_Design" / "Planned_Starter_Cards.md"

SKIP_CLASSES = frozenset(
    {"Sparks", "Yamata_Dragon", "Bladefly", "Muka_Muka", "Jirai_Gumo"}
)

ENUM_TAGS = frozenset(
    {
        "Earth",
        "Water",
        "Wind",
        "Fire",
        "Dark",
        "Light",
        "Fusion",
        "Ritual",
        "Ocean",
        "Insect",
        "Machine",
        "Dragon",
        "Zombie",
        "Fiend",
        "Spellcaster",
        "Warrior",
        "Heal",
        "Draw",
        "Chance",
        "Burn",
        "Normal",
        "Spell",
        "Trap",
        "Banish",
        "WinCon",
        "God",
        "Bundled",
        "Starter",
    }
)

# Display name (after cleaning) -> C# class name
NAME_TO_CLASS: dict[str, str] = {
    "Blank Pendant": "Black_Pendant",
    "Spell Shield Type-B": "Spell_Shield_Type_8",
    "Nightmare's Steelcage": "Nightmare_S_Steelcage",
    "Posessed Dark Soul": "Possessed_Dark_Soul",
    "Queens Double": "Queen_s_Double",
    "Mystical Sheep #1": "Mystical_Sheep_1",
    "Electric Snake": "Electric_Snake",
    "Exarion Universe": "Exarion_Universe",
    "Poison Mummy": "Poison_Mummy",
    "Skull Mark Ladybug": "Skull_Mark_Ladybug",
    "Gravekeepers Curse": "Gravekeeper_s_Curse",
    "The Hunter with 7 Weapons": "The_Hunter_with_7_Weapons",
    "The Fiend Megacyber": "The_Fiend_Megacyber",
    "King of the swamp": "King_of_the_Swamp",
    "Cursed Seal of the Forbidden Spell": "Cursed_Seal_of_the_Forbidden_Spell",
    "Bottomless Trap Hole": "Bottomless_Trap_Hole",
    "Bottomless Shifting Sand": "Bottomless_Shifting_Sand",
    "Beastly Mirror Ritual": "Beastly_Mirror_Ritual",
    "Amazoness Swords Woman": "Amazoness_Swords_Woman",
    "Anti Aircraft Flower": "Anti_Aircraft_Flower",
    "Arcane Archer of the Forest": "Arcane_Archer_of_the_Forest",
    "Balloon Lizard": "Balloon_Lizard",
    "Blast Juggler": "Blast_Juggler",
    "Crass Clown": "Crass_Clown",
    "Dream Clown": "Dream_Clown",
    "Yado Karu": "Yado_Karu",
    "Yomi Ship": "Yomi_Ship",
    "Rigorous Reaver": "Rigorous_Reaver",
    "Gyaku Gire Panda": "Gyaku_Gire_Panda",
    "Jigen Bakudan": "Jigen_Bakudan",
    "Chaos Necromancer": "Chaos_Necromancer",
    "Card Trooper": "Card_Trooper",
    "Jinzo 7": "Jinzo_7",
    "Spirit Reaper": "Spirit_Reaper",
    "Aswan Apparition": "Aswan_Apparition",
    "Magical Plant Mandragola": "Magical_Plant_Mandragola",
    "Great Maju Garzett": "Great_Maju_Garzett",
    "A Cat Of Ill Omen": "A_Cat_of_Ill_Omen",
    "Iron Blacksmith Kotetsu": "Iron_Blacksmith_Kotetsu",
    "Mystic Lamp": "Mystic_Lamp",
    "Nightmare Horse": "Nightmare_Horse",
    "Nubian Guard": "Nubian_Guard",
    "Piranha Army": "Piranha_Army",
    "Servant of Catabolism": "Servant_of_Catabolism",
    "Time Wizard": "Time_Wizard",
    "Winged Minion": "Winged_Minion",
    "Absorbing Kid From The Sky": "Absorbing_Kid_from_the_Sky",
    "Cure Mermaid": "Cure_Mermaid",
    "Emissary of the Afterlife": "Emissary_of_the_Afterlife",
    "Exiled Force": "Exiled_Force",
    "Fairy Guardian": "Fairy_Guardian",
    "Hayabusa Knight": "Hayabusa_Knight",
    "Mad Sword Beast": "Mad_Sword_Beast",
    "Mask of Darkness": "Mask_of_Darkness",
    "Magician of Faith": "Magician_of_Faith",
    "Slate Warrior": "Slate_Warrior",
    "Spirit Caller": "Spirit_Caller",
    "Rite of Spirit": "Rite_of_Spirit",
    "Tainted Wisdom": "Tainted_Wisdom",
    "Theban Nightmare": "Theban_Nightmare",
    "Timeater": "Timeater",
    "Zolga": "Zolga",
    "Draining Shield": "Draining_Shield",
    "Burning Land": "Burning_Land",
    "Versago the Destroyer": "Versago_the_Destroyer",
    "Goddess with the Third Eye": "Goddess_with_the_Third_Eye",
    "Beastking of the Swamps": "Beastking_of_the_Swamps",
    "The Light - Hex-Sealed Fusion": "The_Light_Hex_Sealed_Fusion",
    "The Earth - Hex-Sealed Fusion": "The_Earth_Hex_Sealed_Fusion",
    "The Dark - Hex-Sealed Fusion": "The_Dark_Hex_Sealed_Fusion",
    "Rainbow Flower": "Rainbow_Flower",
    "Double Summon": "Double_Summon",
    "Double_Summon": "Double_Summon",
    "Electric Lizard": "Electric_Lizard",
    "King Tiger Wanghu": "King_Tiger_Wanghu",
    "Dice Jar": "Dice_Jar",
    "Little Wingguard": "Little_Winguard",
    "Ooguchi": "Ooguchi",
    "Leghul": "Leghul",
    "Zone Eater": "Zone_Eater",
    "Minar": "Minar",
    "Bowganian": "Bowganian",
    "Des Feral Imp": "Des_Feral_Imp",
    "Fire Princess": "Fire_Princess",
    "Masked Sorcerer": "Masked_Sorcerer",
    "Mudora": "Mudora",
    "Stone Statue of the Aztecs": "Stone_Statue_of_the_Aztecs",
    "Summoner of Illusions": "Summoner_of_Illusions",
    "Stealth Bird": "Stealth_Bird",
    "Reckless Greed": "Reckless_Greed",
    "Reload": "Reload",
    "Card Destruction": "Card_Destruction",
    "Soul Resurrection": "Soul_Resurrection",
    "The Bistro Butcher": "The_Bistro_Butcher",
    "Axe of Despair": "Axe_of_Despair",
    "Banner of Courage": "Banner_of_Courage",
    "Yellow Luster Shield": "Yellow_Luster_Shield",
    "Call of the Haunted": "Call_of_the_Haunted",
    "Castle Walls": "Castle_Walls",
    "Reinforcements": "Reinforcements",
    "Megamorph": "Megamorph",
    "Poison of the Old Man": "Poison_of_the_Old_Man",
    "Dark Spirit of the Silent": "Dark_Spirit_of_the_Silent",
    "Solemn Wishes": "Solemn_Wishes",
    "Compulsory Evacuation Device": "Compulsory_Evacuation_Device",
    "Hamburger Recipe": "Hamburger_Recipe",
    "Ominous Fortunetelling": "Ominous_Fortunetelling",
    "Curse of Aging": "Curse_of_Aging",
    "Curse of Anubis": "Curse_of_Anubis",
    "Curse of Darkness": "Curse_of_Darkness",
    "Nightmare Wheel": "Nightmare_Wheel",
    "Narrow Pass": "Narrow_Pass",
    "Copycat": "Copycat",
    "Blind Destruction": "Blind_Destruction",
    "Big Bang Shot": "Big_Bang_Shot",
    "Raigeki Break": "Raigeki_Break",
    "Raigeki": "Raigeki",
    "Zombyra the Dark": "Zombyra_the_Dark",
    "Monster Reborn": "Monster_Reborn",
    "Foolish Burial": "Foolish_Burial",
    "Enraged Muka Muka": "Enraged_Muka_Muka",
    "Hourglass of Courage": "Hourglass_of_Courage",
    "Bad Reaction to Simochi": "Bad_Reaction_to_Simochi",
    "Upstart Goblin": "Upstart_Goblin",
    "Dark Mirror Force": "Dark_Mirror_Force",
    "Enchanted Javelin": "Enchanted_Javelin",
    "Stumbling": "Stumbling",
    "Tornado Wall": "Tornado_Wall",
    "Skull Invitation": "Skull_Invitation",
    "Shield & Sword": "Shield_Sword",
    "Secret Barrel": "Secret_Barrel",
    "Dragon Nails": "Dragon_Nails",
    "Spellbook Organization": "Spellbook_Organization",
    "Cestus of Dagla": "Cestus_of_Dagla",
    "Dark Hole": "Dark_Hole",
    "Des Counterblow": "Des_Counterblow",
    "Emergency Provisions": "Emergency_Provisions",
    "Fairy Box": "Fairy_Box",
    "Polymerization": "Polymerization",
    "Rope of Life": "Rope_of_Life",
    "Rush Recklessly": "Rush_Recklessly",
    "The Reliable Guardian": "The_Reliable_Guardian",
    "Spellbinding Circle": "Spellbinding_Circle",
    "Skull Dice": "Skull_Dice",
    "Reinforcement of the Army": "Reinforcement_of_the_Army",
    "Cost Down": "Cost_Down",
    "Book of Moon": "Book_of_Moon",
    "Gravity Axe Grarl": "Gravity_Axe_Grarl",
    "Millennium Shield": "Millennium_Shield",
    "Big Shield Gardna": "Big_Shield_Gardna",
    "Terrorking Archfiend": "Terrorking_Archfiend",
    "Convulsion of Nature": "Convulsion_of_Nature",
    "Dark Snake Syndrome": "Dark_Snake_Syndrome",
    "Pot of Greed": "Pot_Of_Greed",
    "Ameba": "Ameba",
}


def slug_class(name: str) -> str:
    s = name.replace("&", "and").replace("#", "").strip()
    s = re.sub(r"\s+", "_", s)
    s = re.sub(r"[^A-Za-z0-9_]", "", s)
    return s


def clean_display_name(raw_left: str) -> str:
    s = raw_left.strip()
    s = re.sub(r"\s*-\s*\d+\s*Cost.*$", "", s, flags=re.I)
    while True:
        nxt = re.sub(r"\s*-\s*(Rare|Uncommon|Common)\s*$", "", s, flags=re.I)
        nxt = re.sub(r"\s*-\s*$", "", nxt)
        if nxt == s:
            break
        s = nxt.strip()
    return s.strip()


def parse_planned_line(line: str) -> tuple[str, list[str], str | None] | None:
    line = line.strip()
    if not line or line.startswith("Idea ") or line.startswith("Narrow Pass 3"):
        return None
    line = re.sub(r"\([^)]*\)", "", line)
    parts = [p.strip() for p in line.split("|")]
    raw_name = parts[0]
    if not raw_name or raw_name.lower().startswith("copycat - rare"):
        return None
    rarity: str | None = None
    if re.search(r"\bRare\b", raw_name, re.I):
        rarity = "Rare"
    elif re.search(r"\bUncommon\b", raw_name, re.I):
        rarity = "Uncommon"
    elif re.search(r"\bCommon\b", raw_name, re.I):
        rarity = "Common"
    name = clean_display_name(raw_name)
    if not name:
        return None
    tokens: list[str] = []
    for p in parts[1:]:
        t = p.strip()
        if not t or t.startswith("-") and "Bundles" in t:
            continue
        if t.lower() in ("rare", "uncommon", "common"):
            rarity = t.capitalize() if t.lower() == "common" else t.title()
            continue
        # drop design notes after " - "
        t = t.split(" - ")[0].strip()
        if not t:
            continue
        for word in re.split(r"[\s/]+", t):
            w = word.strip()
            if not w:
                continue
            if w in ("Rare", "Uncommon", "Common"):
                rarity = w
                continue
            tokens.append(w)
    return name, tokens, rarity


def tokens_to_pack_tags(tokens: list[str]) -> list[str]:
    """Every row in Planned_Starter_Cards.md (lines processed by main) is a starter-pool card."""
    found: set[str] = set()
    for w in tokens:
        key = w.strip()
        # Title Case for enum
        for tag in ENUM_TAGS:
            if tag.lower() == key.lower():
                found.add(tag)
                break
    found.add("Starter")
    order = ["Starter"]
    order.extend(sorted(t for t in found if t != "Starter"))
    return order


def format_pack_expr(tags: list[str]) -> str:
    if not tags:
        return "YgoCardPackTags.None"
    parts = [f"YgoCardPackTags.{t}" for t in tags]
    if len(parts) == 1:
        return parts[0]
    return " | ".join(parts)


def build_block(class_name: str, pack_expr: str) -> str:
    return f"""
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => {pack_expr};

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //}};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {{
        typeof({class_name}),
    }};
"""


def find_constructor_insertion_point(text: str, class_name: str) -> int | None:
    needle = f"public {class_name}("
    i = text.find(needle)
    if i < 0:
        return None
    j = text.find("{", i)
    if j < 0:
        return None
    depth = 0
    for k in range(j, len(text)):
        c = text[k]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return k + 1
    return None


def strip_pack_section(text: str) -> str:
    start = text.find("    // Dictates the card pack tags this card will be included in.")
    if start < 0:
        return text
    rc = text.find("public override Type[] RelatedCards", start)
    if rc < 0:
        semi = text.find(";", text.find("public override YgoCardPackTags PackTags", start))
        if semi < 0:
            return text
        end = semi + 1
        while end < len(text) and text[end] in "\r\n":
            end += 1
        return text[:start] + text[end:]
    j = text.find("{", rc)
    depth = 0
    end = None
    for k in range(j, len(text)):
        if text[k] == "{":
            depth += 1
        elif text[k] == "}":
            depth -= 1
            if depth == 0:
                sem = text.find(";", k)
                end = sem + 1 if sem >= 0 else k + 1
                break
    if end is None:
        return text
    while end < len(text) and text[end] in "\r\n":
        end += 1
    return text[:start] + text[end:]


def ensure_using_system(text: str) -> str:
    if re.search(r"^using System;", text, re.M):
        return text
    m = re.search(r"^using ", text, re.M)
    if not m:
        return "using System;\n" + text
    return text[: m.start()] + "using System;\n" + text[m.start() :]


def ensure_using_cards_namespace(text: str) -> str:
    if "YgoCardPackTags" not in text:
        return text
    if "using YgoDuelist.YgoDuelistCode.Cards;" in text:
        return text
    m = re.search(r"^using ", text, re.M)
    if m:
        return text[: m.start()] + "using YgoDuelist.YgoDuelistCode.Cards;\n" + text[m.start() :]
    return text


def patch_rarity(text: str, class_name: str, rarity: str | None) -> str:
    if rarity not in ("Rare", "Uncommon", "Common"):
        return text
    needle = f"public {class_name}("
    i = text.find(needle)
    if i < 0:
        return text
    j = text.find("{", i)
    if j < 0:
        return text
    segment = text[i:j]
    new_seg = re.sub(
        r"CardRarity\.\w+",
        f"CardRarity.{rarity}",
        segment,
        count=1,
    )
    return text[:i] + new_seg + text[j:]


def collect_classes() -> dict[str, Path]:
    pat = re.compile(r"public sealed class (\w+)")
    m: dict[str, Path] = {}
    for p in CARDS.rglob("*.cs"):
        if "Command" in p.parts:
            continue
        t = p.read_text(encoding="utf-8")
        for mo in pat.finditer(t):
            m[mo.group(1)] = p
    return m


def resolve_class(display_name: str, classes: dict[str, Path]) -> str | None:
    if display_name in NAME_TO_CLASS:
        cn = NAME_TO_CLASS[display_name]
        if cn in classes:
            return cn
    slug = slug_class(display_name)
    if slug in classes:
        return slug
    if display_name.replace(" ", "_") in classes:
        return display_name.replace(" ", "_")
    return None


def main() -> None:
    lines = PLANNED.read_text(encoding="utf-8").splitlines()
    classes = collect_classes()
    missing: list[str] = []
    patched: list[str] = []
    for idx, line in enumerate(lines[:178], start=1):
        parsed = parse_planned_line(line)
        if not parsed:
            continue
        display_name, tokens, rarity = parsed
        class_name = resolve_class(display_name, classes)
        if not class_name:
            missing.append(f"L{idx}: {display_name!r}")
            continue
        if class_name in SKIP_CLASSES:
            continue
        path = classes[class_name]
        text = path.read_text(encoding="utf-8")
        tag_list = tokens_to_pack_tags(tokens)
        pack_expr = format_pack_expr(tag_list)
        block = build_block(class_name, pack_expr)
        text = strip_pack_section(text)
        insert_at = find_constructor_insertion_point(text, class_name)
        if insert_at is None:
            missing.append(f"L{idx}: no ctor {class_name}")
            continue
        text = text[:insert_at] + block + text[insert_at:]
        text = patch_rarity(text, class_name, rarity)
        text = ensure_using_system(text)
        text = ensure_using_cards_namespace(text)
        path.write_text(text, encoding="utf-8")
        patched.append(class_name)
    print("Patched", len(patched), "files")
    if missing:
        print("Missing / skipped ctor:")
        for x in missing:
            print(" ", x)


if __name__ == "__main__":
    main()
