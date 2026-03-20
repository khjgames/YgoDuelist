"""One-off: list YgoDuelist card classes outside Todo that match cards.json entries."""
import json
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
CARDS_ROOT = REPO / "YgoDuelistCode" / "Cards"
CARDS_JSON = REPO / "cards.json"


def normalized(s: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", (s or "").lower())


def main() -> None:
    with open(CARDS_JSON, encoding="utf-8") as f:
        data = json.load(f)

    json_by_norm: dict[str, list[str]] = {}
    for o in data:
        n = normalized(o.get("name", ""))
        json_by_norm.setdefault(n, []).append(o.get("name"))

    implemented: list[tuple[str, str, str]] = []
    for p in CARDS_ROOT.rglob("*.cs"):
        if "Todo" in p.parts:
            continue
        stem = p.stem
        rel = str(p.relative_to(REPO))
        implemented.append((stem, normalized(stem), rel))

    json_norms = set(json_by_norm.keys())
    hits: list[tuple[str, str | list[str], str]] = []
    misses: list[tuple[str, str]] = []
    for stem, norm, rel in sorted(implemented, key=lambda x: x[2]):
        if norm in json_norms:
            names = json_by_norm[norm]
            hits.append((stem, names, rel))
        else:
            misses.append((stem, rel))

    print(f"=== Implemented outside Todo that ARE in cards.json ({len(hits)}) ===")
    for stem, names, rel in hits:
        disp = names[0] if len(names) == 1 else f"{names[0]} (json: {len(names)} same norm)"
        print(f"  {stem}  <-  \"{disp}\"  ({rel})")

    print()
    print(f"=== Implemented outside Todo NOT in cards.json ({len(misses)}) ===")
    for stem, rel in misses:
        print(f"  {stem}  ({rel})")


if __name__ == "__main__":
    main()
