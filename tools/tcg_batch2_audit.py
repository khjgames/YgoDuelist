"""TCG pierce/direct audit for inventory batch 2 (implemented[100:200]) vs cards_database.json."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def main() -> None:
    with open(ROOT / "docs/card_inventory_data.json", encoding="utf-8") as f:
        impl = json.load(f)["implemented"][100:200]

    with open(ROOT / "cards_database.json", encoding="utf-8") as f:
        db = json.load(f)

    by_name = {(c.get("name") or "").lower(): c for c in db}

    def find_card(class_name: str):
        variants = [
            class_name.replace("_", " "),
            class_name.replace("_", "-"),
            re.sub(r"_s_", "'s ", class_name).replace("_", " "),
        ]
        for v in variants:
            cl = v.lower()
            if cl in by_name:
                return by_name[cl]
        cn = class_name.replace("_", " ").lower()
        for k, v in by_name.items():
            if k.replace("-", " ") == cn:
                return v
        return None

    pierce_re = re.compile(r"piercing battle damage", re.I)
    direct_re = re.compile(
        r"attack (your opponent |directly|the opponent's life|life points directly)|attack directly",
        re.I,
    )

    monsters = [e for e in impl if e.get("kind") == "EffectMonster"]
    print(f"Batch 2 EffectMonster rows in inventory: {len(monsters)}")
    print()

    print("Per-entry TCG match + pierce/direct (explicit TCG name from DB):")
    for e in impl:
        cn = e["className"]
        kind = e.get("kind", "")
        card = find_card(cn)
        if not card:
            print(f"{cn}\t{kind}\tNO_DB_MATCH")
            continue
        tcg_name = card.get("name", "")
        desc = (card.get("desc") or "") + " " + tcg_name
        low = desc.lower()
        p = bool(pierce_re.search(desc)) and "ear-piercing" not in low
        d = bool(direct_re.search(desc)) or "life points directly" in low
        flag = ""
        if p or d:
            flag = f"\tP={p}\tD={d}"
        print(f"{cn}\t{kind}\t{tcg_name!r}{flag}")

    print()
    print("--- EffectMonster-only TCG pierce/direct (should be empty for batch 2) ---")
    for e in impl:
        if e.get("kind") != "EffectMonster":
            continue
        cn = e["className"]
        card = find_card(cn)
        if not card:
            print(f"{cn}\tNO_DB")
            continue
        desc = (card.get("desc") or "") + " " + (card.get("name") or "")
        low = desc.lower()
        p = bool(pierce_re.search(desc)) and "ear-piercing" not in low
        d = bool(direct_re.search(desc)) or "life points directly" in low
        if p or d:
            print(f"{cn}\tP={p}\tD={d}\t{card['name']!r}")


if __name__ == "__main__":
    main()
