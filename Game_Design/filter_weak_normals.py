"""List Normal Monsters matching ATK/DEF caps and level range; sorted by max(ATK, DEF) descending."""

import json
from pathlib import Path

DB = Path(__file__).resolve().parent / "cards_database.json"
OUT_MD = Path(__file__).resolve().parent / "weak_normal_monsters_atk1900_def1700_level56.md"

ATK_MAX_EXCL = 1900
DEF_MAX_EXCL = 1700
LEVEL_MIN = 5
LEVEL_MAX = 6


def main() -> None:
    with DB.open(encoding="utf-8") as f:
        cards = json.load(f)

    matches = []
    for c in cards:
        if c.get("type") != "Normal Monster":
            continue
        atk, df, level = c.get("atk"), c.get("def"), c.get("level")
        if atk is None or df is None:
            continue
        if atk >= ATK_MAX_EXCL or df >= DEF_MAX_EXCL or level < LEVEL_MIN or level > LEVEL_MAX:
            continue
        hi = max(atk, df)
        matches.append((hi, atk, df, c["name"], c.get("id")))

    matches.sort(key=lambda t: (-t[0], -t[1], t[3]))

    print(f"Count: {len(matches)}\n")
    for hi, atk, df, name, cid in matches:
        print(f"{hi:4d}  ATK {atk:4d} / DEF {df:4d}  |  {name}  (id {cid})")

    lines = [
        f"# Normal monsters: Level {LEVEL_MIN}–{LEVEL_MAX}, ATK under {ATK_MAX_EXCL}, DEF under {DEF_MAX_EXCL}",
        "",
        f"From `cards_database.json`. Type `Normal Monster`, numeric stats, "
        f"Level {LEVEL_MIN}–{LEVEL_MAX} inclusive, ATK strictly under {ATK_MAX_EXCL}, "
        f"DEF strictly under {DEF_MAX_EXCL}. "
        "Sorted by `max(ATK, DEF)` descending (then ATK, then name).",
        "",
        f"**Count:** {len(matches)}",
        "",
        "| max | ATK | DEF | Name | ID |",
        "|-----|-----|-----|------|-----|",
    ]
    for hi, atk, df, name, cid in matches:
        lines.append(f"| {hi} | {atk} | {df} | {name} | {cid} |")
    lines.extend(["", "Regenerate: `python filter_weak_normals.py` in this folder."])
    OUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"\nWrote {OUT_MD.name}")


if __name__ == "__main__":
    main()
