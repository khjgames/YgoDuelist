"""
Regenerate Spell/Todo/Ritual/*.cs from cards_database.json (Ritual Spell Card race).

Run from repo root:
  python tools/sync_ritual_spells_from_db.py
  python tools/sync_ritual_spells_from_db.py --write
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "cards_database.json"
RITUAL_DIR = ROOT / "YgoDuelistCode" / "Cards" / "Spell" / "Todo" / "Ritual"

sys.path.insert(0, str(ROOT / "tools"))
from generate_missing_cards_from_db import norm_key, spell_race_to_folder_and_race, stem_to_class_name  # noqa: E402
from ritual_spell_from_db import build_card_name_to_class, parse_ritual_spell_spec, render_ritual_spell_cs  # noqa: E402


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true")
    args = ap.parse_args()

    with open(DB_PATH, "r", encoding="utf-8") as f:
        data: list[dict] = json.load(f)

    name_to_cls = build_card_name_to_class(data)
    seen_stems: set[str] = set()
    errors: list[str] = []
    planned: list[tuple[Path, str, str]] = []

    for card in data:
        fn = card.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        nk = norm_key(stem)
        if nk in seen_stems:
            continue
        seen_stems.add(nk)

        if (card.get("type") or "") != "Spell Card":
            continue
        sub, _, dr = spell_race_to_folder_and_race(card.get("race"))
        if sub != "Ritual" or dr != "SpellRitual":
            continue

        spell_cls = stem_to_class_name(stem)
        path = RITUAL_DIR / f"{spell_cls}.cs"
        if not path.exists():
            continue

        try:
            spec = parse_ritual_spell_spec(card.get("desc") or "", name_to_cls)
        except ValueError as e:
            errors.append(f"{spell_cls}: {e}")
            continue

        planned.append((path, spell_cls, render_ritual_spell_cs(spell_cls, spec)))

    for err in errors:
        print("ERROR:", err)
    if errors:
        sys.exit(1)

    print("Ritual spell files to sync:", len(planned))
    for path, cls, _ in planned:
        print(" ", cls)

    if args.write:
        for path, _, body in planned:
            path.write_text(body, encoding="utf-8")
            print("wrote", path.relative_to(ROOT))


if __name__ == "__main__":
    main()
