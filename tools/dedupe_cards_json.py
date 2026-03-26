"""
Remove redundant localization keys: when both `CARDID.suffix` and `YGODUELIST-CARDID.suffix`
exist with identical values, keep only `YGODUELIST-CARDID.suffix`.

Run from repo root:
  python tools/dedupe_cards_json.py
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

CARDS_JSON = (
    Path(__file__).resolve().parent.parent
    / "YgoDuelist"
    / "localization"
    / "eng"
    / "cards.json"
)


def main() -> int:
    if not CARDS_JSON.is_file():
        print(f"Missing {CARDS_JSON}", file=sys.stderr)
        return 1
    data: dict[str, str] = json.loads(CARDS_JSON.read_text(encoding="utf-8"))
    removed: list[str] = []
    for k in list(data.keys()):
        if k.startswith("YGODUELIST-"):
            continue
        yk = "YGODUELIST-" + k
        if yk in data and data[yk] == data[k]:
            del data[k]
            removed.append(k)
    CARDS_JSON.write_text(
        json.dumps(data, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Removed {len(removed)} duplicate keys (kept YGODUELIST- canonical).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
