import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
db = json.load(open(ROOT / "cards_database.json", encoding="utf-8"))
by_name = {c["name"]: c for c in db}

NAMES = [
    "Amazoness Blowpiper",
    "Amazoness Swords Woman",
    "Amazoness Tiger",
    "Ameba",
    "Anti-Aircraft Flower",
    "Arcane Archer of the Forest",
    "Bladefly",
    "Blast Juggler",
    "Bowganian",
    "Burning Algae",
    "Cat's Ear Tribe",
    "Chaos Command Magician",
    "Crass Clown",
    "Cure Mermaid",
    "Cyber Jar",
    "D.D. Crazy Beast",
    "D.D. Warrior Lady",
    "Dancing Fairy",
    "Dark Cat with White Tail",
    "Dark Jeroid",
    "Dark Zebra",
    "Des Kangaroo",
    "Dream Clown",
    "Exiled Force",
    "Fairy Guardian",
    "Hoshiningen",
    "Jinzo #7",
    "Little Chimera",
    "Lord of D.",
    "Milus Radiant",
    "Muka Muka",
    "Possessed Dark Soul",
    "Star Boy",
    "Sword Hunter",
    "Tainted Wisdom",
    "Terrorking Archfiend",
    "The Legendary Fisherman",
    "Thunder Dragon",
    "Toon Dark Magician Girl",
    "Toon Mermaid",
    "Toon Summoned Skull",
    "Witch's Apprentice",
    "Zombyra the Dark",
    "Zone Eater",
]

pierce_re = re.compile(r"piercing battle damage", re.I)
direct_re = re.compile(
    r"attack (your opponent )?directly|life points directly|directly, unless",
    re.I,
)

for n in NAMES:
    c = by_name.get(n)
    if not c:
        print("MISSING", n)
        continue
    d = c.get("desc") or ""
    low = d.lower()
    p = bool(pierce_re.search(d)) and "ear-piercing" not in low
    di = bool(direct_re.search(d))
    if p or di:
        print(n, "PIERCE" if p else "", "DIRECT" if di else "", d[:120].replace("\n", " "))
