"""Audit first-100 inventory Effect monsters vs cards_database.json for piercing / direct attack."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

# ClassName -> exact TCG name in cards_database.json
NAME_OVERRIDES: dict[str, str] = {
    "Cat_s_Ear_Tribe": "Cat's Ear Tribe",
    "D_D_Crazy_Beast": "D.D. Crazy Beast",
    "D_D_Warrior_Lady": "D.D. Warrior Lady",
    "Jinzo_7": "Jinzo #7",
    "Lord_of_D": "Lord of D.",
    "Witchs_Apprentice": "Witch's Apprentice",
    "The_Legendary_Fisherman": "The Legendary Fisherman",
    "Toon_Dark_Magician_Girl": "Toon Dark Magician Girl",
    "Toon_Mermaid": "Toon Mermaid",
    "Toon_Summoned_Skull": "Toon Summoned Skull",
    "Zombyra_the_Dark": "Zombyra the Dark",
    "Anti_Aircraft_Flower": "Anti-Aircraft Flower",
    "Arcane_Archer_of_the_Forest": "Arcane Archer of the Forest",
    "Blast_Juggler": "Blast Juggler",
    "Burning_Algae": "Burning Algae",
    "Chaos_Command_Magician": "Chaos Command Magician",
    "Crass_Clown": "Crass Clown",
    "Cure_Mermaid": "Cure Mermaid",
    "Cyber_Jar": "Cyber Jar",
    "Dancing_Fairy": "Dancing Fairy",
    "Dark_Cat_with_White_Tail": "Dark Cat with White Tail",
    "Dark_Jeroid": "Dark Jeroid",
    "Dark_Zebra": "Dark Zebra",
    "Des_Kangaroo": "Des Kangaroo",
    "Dream_Clown": "Dream Clown",
    "Enraged_Muka_Muka": "Enraged Muka Muka",
    "Exiled_Force": "Exiled Force",
    "Fairy_Guardian": "Fairy Guardian",
    "Little_Chimera": "Little Chimera",
    "Milus_Radiant": "Milus Radiant",
    "Muka_Muka": "Muka Muka",
    "Possessed_Dark_Soul": "Possessed Dark Soul",
    "Star_Boy": "Star Boy",
    "Sword_Hunter": "Sword Hunter",
    "Tainted_Wisdom": "Tainted Wisdom",
    "Terrorking_Archfiend": "Terrorking Archfiend",
    "Thunder_Dragon": "Thunder Dragon",
    "Zone_Eater": "Zone Eater",
    "Amazoness_Blowpiper": "Amazoness Blowpiper",
    "Amazoness_Swords_Woman": "Amazoness Swords Woman",
    "Amazoness_Tiger": "Amazoness Tiger",
    "Ameba": "Ameba",
    "Bladefly": "Bladefly",
    "Bowganian": "Bowganian",
    "Hoshiningen": "Hoshiningen",
}


def default_tcg_name(class_name: str) -> str:
    if class_name in NAME_OVERRIDES:
        return NAME_OVERRIDES[class_name]
    s = class_name.replace("_", " ")
    s = re.sub(r"(\w) s (\w)", r"\1's \2", s)  # rough
    return s


def main() -> None:
    with open(ROOT / "docs/card_inventory_data.json", encoding="utf-8") as f:
        impl = json.load(f)["implemented"][:100]

    monsters = [e for e in impl if e.get("kind") == "EffectMonster"]

    with open(ROOT / "cards_database.json", encoding="utf-8") as f:
        db = json.load(f)

    by_lower = {(c.get("name") or "").lower(): c for c in db}

    pierce_re = re.compile(r"piercing battle damage", re.I)
    direct_re = re.compile(
        r"(can attack (your opponent )?directly|attack (your opponent )?directly|attack directly|life points directly)",
        re.I,
    )

    rows = []
    for e in monsters:
        cn = e["className"]
        tcg = default_tcg_name(cn)
        card = by_lower.get(tcg.lower())
        if not card:
            rows.append((cn, tcg, None, False, False, "NO_MATCH"))
            continue
        desc = (card.get("desc") or "") + "\n" + (card.get("name") or "")
        low = desc.lower()
        if "ear-piercing" in low or "ear piercing" in low:
            p = False
        else:
            p = bool(pierce_re.search(desc))
        d = bool(direct_re.search(desc)) or "life points directly" in low
        rows.append((cn, tcg, card.get("type"), p, d, ""))

    for cn, tcg, typ, p, d, err in rows:
        flags = []
        if p:
            flags.append("Splinter")
        if d:
            flags.append("Blighted")
        fstr = "+".join(flags) if flags else "-"
        print(f"{cn}\t{tcg}\t{fstr}\t{err}")


if __name__ == "__main__":
    main()
