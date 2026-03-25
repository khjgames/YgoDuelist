"""
Add YGODUELIST-*.title / description keys to cards.json for every cards_database row
that maps to a C# class name (same stem rules as generate_missing_cards_from_db.py).

Run: python tools/seed_card_locales_from_db.py --write
Fusion text from DB: python tools/seed_card_locales_from_db.py --write --refresh-fusion-locales
Ritual text from DB: python tools/seed_card_locales_from_db.py --write --refresh-ritual-locales
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "cards_database.json"
CARDS_JSON = ROOT / "YgoDuelist" / "localization" / "eng" / "cards.json"

# Import naming helpers from sibling tool
sys.path.insert(0, str(ROOT / "tools"))
from generate_missing_cards_from_db import (  # noqa: E402
    monster_kind,
    norm_key,
    spell_race_to_folder_and_race,
    stem_to_class_name,
    trap_race_to_folder_and_race,
)


def entry_from_class(cls: str) -> str:
    return "YGODUELIST-" + cls.upper()


def lines_monster(entry: str, title: str) -> dict[str, str]:
    base = entry.removeprefix("YGODUELIST-")
    return {
        f"YGODUELIST-{base}.title": title,
        f"YGODUELIST-{base}.description": (
            "Deal [blue]{Damage}[/blue] damage.\n"
            "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]"
        ),
        f"YGODUELIST-{base}.description_skill": (
            "Gain [blue]{Block}[/blue] block.\n"
            "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]"
        ),
        f"YGODUELIST-{base}.description_combat": (
            "Deal [blue]{CalculatedATK}[/blue] damage.\n"
            "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]"
        ),
        f"YGODUELIST-{base}.description_skill_combat": (
            "Gain [blue]{CalculatedDEF}[/blue] block.\n"
            "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]"
        ),
    }


def fusion_card_text_from_desc(desc: str) -> str:
    """First paragraph of YGO card text (recipe + start of effect), normalized newlines."""
    if not desc:
        return ""
    t = desc.replace("\r\n", "\n").strip()
    para = t.split("\n\n", 1)[0].strip()
    if len(para) > 600:
        para = para[:597] + "..."
    return para


def lines_fusion_from_db(entry: str, title: str, desc: str) -> dict[str, str]:
    base = entry.removeprefix("YGODUELIST-")
    recipe = fusion_card_text_from_desc(desc)
    mid = "\n" + recipe + "\n" if recipe else "\n"
    return {
        f"YGODUELIST-{base}.title": title,
        f"YGODUELIST-{base}.description": "Deal [blue]{Damage}[/blue] damage." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]",
        f"YGODUELIST-{base}.description_skill": "Gain [blue]{Block}[/blue] block." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]",
        f"YGODUELIST-{base}.description_combat": "Deal [blue]{CalculatedATK}[/blue] damage." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]",
        f"YGODUELIST-{base}.description_skill_combat": "Gain [blue]{CalculatedDEF}[/blue] block." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]",
    }


def lines_ritual_monster_from_db(entry: str, title: str, desc: str) -> dict[str, str]:
    """Ritual monster: YGO text (summon clause + effect lead) between combat lines, like fusion."""
    base = entry.removeprefix("YGODUELIST-")
    ygo = fusion_card_text_from_desc(desc)
    mid = "\n" + ygo + "\n" if ygo else "\n"
    return {
        f"YGODUELIST-{base}.title": title,
        f"YGODUELIST-{base}.description": "Deal [blue]{Damage}[/blue] damage." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]",
        f"YGODUELIST-{base}.description_skill": "Gain [blue]{Block}[/blue] block." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]",
        f"YGODUELIST-{base}.description_combat": "Deal [blue]{CalculatedATK}[/blue] damage." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]",
        f"YGODUELIST-{base}.description_skill_combat": "Gain [blue]{CalculatedDEF}[/blue] block." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]",
    }


def lines_ritual_spell_from_db(entry: str, title: str, desc: str) -> dict[str, str]:
    base = entry.removeprefix("YGODUELIST-")
    text = fusion_card_text_from_desc(desc)
    if not text:
        text = "Ritual Spell (no description in database)."
    return {
        f"YGODUELIST-{base}.title": title,
        f"YGODUELIST-{base}.description": text,
    }


def lines_spell_trap(entry: str, title: str) -> dict[str, str]:
    base = entry.removeprefix("YGODUELIST-")
    return {
        f"YGODUELIST-{base}.title": title,
        f"YGODUELIST-{base}.description": "TODO: implement card effect (see Cards_Revised.md where listed).",
    }


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true")
    ap.add_argument(
        "--refresh-fusion-locales",
        action="store_true",
        help="Overwrite fusion monster locale strings with cards_database desc (first paragraph).",
    )
    ap.add_argument(
        "--refresh-ritual-locales",
        action="store_true",
        help="Overwrite ritual monster + Ritual Spell locale strings from cards_database desc.",
    )
    args = ap.parse_args()

    with open(DB_PATH, "r", encoding="utf-8") as f:
        data: list[dict] = json.load(f)

    updates: dict[str, str] = {}
    fusion_refresh: dict[str, str] = {}
    ritual_refresh: dict[str, str] = {}
    seen: set[str] = set()

    for card in data:
        fn = card.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        nk = norm_key(stem)
        if nk in seen:
            continue
        seen.add(nk)

        cls = stem_to_class_name(stem)
        ent = entry_from_class(cls)
        title = card.get("name") or cls.replace("_", " ")
        ctype = card.get("type") or ""

        if ctype == "Spell Card":
            updates.update(lines_spell_trap(ent, title))
        elif ctype == "Trap Card":
            updates.update(lines_spell_trap(ent, title))
        else:
            kind = monster_kind(ctype)
            if kind == "fusion":
                flines = lines_fusion_from_db(ent, title, card.get("desc") or "")
                fusion_refresh.update(flines)
                updates.update(flines)
            elif kind == "ritual":
                rml = lines_ritual_monster_from_db(ent, title, card.get("desc") or "")
                ritual_refresh.update(rml)
                updates.update(rml)
            else:
                updates.update(lines_monster(ent, title))

    with open(CARDS_JSON, "r", encoding="utf-8") as f:
        loc: dict[str, str] = json.load(f)

    missing = {k: v for k, v in updates.items() if k not in loc}
    print("Locale keys in DB map:", len(updates))
    print("Keys missing from cards.json:", len(missing))

    if args.write:
        loc.update(missing)
        if args.refresh_fusion_locales:
            loc.update(fusion_refresh)
            print("Refreshed fusion locale keys:", len(fusion_refresh))
        if args.refresh_ritual_locales:
            loc.update(ritual_refresh)
            print("Refreshed ritual locale keys:", len(ritual_refresh))
        with open(CARDS_JSON, "w", encoding="utf-8") as f:
            json.dump(loc, f, indent=2, ensure_ascii=False, sort_keys=True)
            f.write("\n")
        print("Wrote", CARDS_JSON)
    elif missing:
        print("Use --write to add missing keys.")
    if args.refresh_fusion_locales and not args.write:
        print("Use --write with --refresh-fusion-locales to apply fusion text updates.")
    if args.refresh_ritual_locales and not args.write:
        print("Use --write with --refresh-ritual-locales to apply ritual text updates.")


if __name__ == "__main__":
    main()
