import json
import re
from pathlib import Path

p = Path(__file__).with_name("cards_database.json")
j = json.loads(p.read_text(encoding="utf-8"))
fusions = [c for c in j if c.get("type") == "Fusion Monster"]
# First line is only quoted card names joined by +
named_only = re.compile(r'^(\s*"[^"]+"\s*\+\s*)+"[^"]+"\s*$')

non_named = []
for c in fusions:
    desc = c.get("desc") or ""
    first = desc.split("\n")[0].split("\r")[0].strip()
    if not first:
        continue
    if named_only.match(first):
        continue
    non_named.append((c["name"], first, desc))

print(f"Fusion monsters total: {len(fusions)}")
print(f"With non-(quoted-name-only) first line: {len(non_named)}\n")
for name, first, desc in sorted(non_named, key=lambda x: x[0]):
    print("---", name)
    print("  First line:", repr(first))
    print()

# Mixed: first line has quoted names AND requirement-style tokens
req_token = re.compile(
    r"\d+\s+(?:Level\s+)?(?:\d+\s+)?(?:[A-Z]+|Aqua|Beast|Cyberse|Dinosaur|Dragon|Fairy|Fiend|Fish|Insect|Machine|Plant|Psychic|Pyro|Reptile|Rock|Sea Serpent|Spellcaster|Thunder|Warrior|Winged Beast|Wyrm|Zombie)\s+monster",
    re.I,
)
mixed = []
for c in fusions:
    desc = c.get("desc") or ""
    first = desc.split("\n")[0].split("\r")[0].strip()
    if '"' not in first:
        continue
    if not req_token.search(first) and not re.search(
        r"\d+\s+Dragon\s+monsters?", first, re.I
    ):
        # also "1 Aqua monster" style
        if not re.search(r"\d+\s+\w+\s+monster", first, re.I):
            continue
    if named_only.match(first):
        continue
    mixed.append((c["name"], first))

print("--- MIXED (quoted names + requirement text on first line) ---")
print("COUNT", len(mixed))
for name, first in sorted(mixed, key=lambda x: x[0]):
    print(name, "->", repr(first))
