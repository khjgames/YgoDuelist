"""
Map Cards_Revised.md card names -> cards_database.json row -> C# implementation file (if present).

This is a reporting tool to ensure we can systematically implement all revised effects.
"""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "cards_database.json"
CARDS_CS_ROOT = ROOT / "YgoDuelistCode" / "Cards"

# sibling helper
import sys

sys.path.insert(0, str(ROOT / "tools"))
from generate_missing_cards_from_db import norm_key, stem_to_class_name  # noqa: E402


def normalize_name(s: str) -> str:
    s = s.strip().lower()
    s = re.sub(r"\s+", " ", s)
    return s


def scan_class_to_file() -> dict[str, Path]:
    out: dict[str, Path] = {}
    for path in CARDS_CS_ROOT.rglob("*.cs"):
        if "Command" in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        m = re.search(r"public\s+sealed\s+class\s+(\w+)\s*:", text)
        if not m:
            continue
        cls = m.group(1)
        out[cls] = path
        if cls.startswith("Card_"):
            out.setdefault(cls[5:], path)
    return out


def main() -> None:
    revised = (ROOT / "tools" / "extract_revised_cards.py").read_text(encoding="utf-8")
    # not importing to keep this standalone; parse list from running output format (first line is count)
    # Instead, read Cards_Revised.md directly similarly.
    md_lines = (ROOT / "Cards_Revised.md").read_text(encoding="utf-8").splitlines()
    cards: list[str] = []
    for ln in md_lines:
        s = ln.strip()
        if s.lower().startswith("generic upgrade"):
            break
        if not s or s.startswith("Listing") or s.startswith("You will") or s.startswith("Important"):
            continue
        if "->" in s:
            cards.append(s.split("->", 1)[0].strip())
        elif " - " in s and s[0].isalpha():
            cards.append(s.split(" - ", 1)[0].strip())

    seen: set[str] = set()
    cards2: list[str] = []
    for c in cards:
        k = c.lower()
        if k in seen:
            continue
        seen.add(k)
        cards2.append(c)

    data = json.loads(DB_PATH.read_text(encoding="utf-8"))
    db_by_name: dict[str, dict] = {}
    for row in data:
        name = row.get("name")
        if not name:
            continue
        db_by_name[normalize_name(name)] = row

    class_to_file = scan_class_to_file()

    missing_db: list[str] = []
    missing_cs: list[str] = []
    ok: list[str] = []
    for name in cards2:
        row = db_by_name.get(normalize_name(name))
        if not row:
            missing_db.append(name)
            continue
        stem = (row.get("image_filename") or "").removesuffix(".jpg")
        cls = stem_to_class_name(stem) if stem else ""
        # try class match by norm_key against existing classes
        # best effort: direct class name, else any class whose norm_key matches the stem
        found: Path | None = class_to_file.get(cls)
        if not found:
            nk = norm_key(stem)
            for ccls, p in class_to_file.items():
                if norm_key(ccls) == nk or (ccls.startswith("Card_") and norm_key(ccls[5:]) == nk):
                    found = p
                    cls = ccls
                    break
        if not found:
            missing_cs.append(f"{name} -> {stem} -> {cls}")
            continue
        ok.append(f"{name}\t{row.get('type')}\t{cls}\t{found.relative_to(ROOT)}")

    print("OK:", len(ok))
    for line in ok:
        print(line)
    print("\nMissing in DB:", len(missing_db))
    for n in missing_db:
        print(n)
    print("\nMissing C# class:", len(missing_cs))
    for n in missing_cs:
        print(n)


if __name__ == "__main__":
    main()

