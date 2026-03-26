"""Build docs/IMPLEMENTED_CARDS.md from card_inventory_data.json + Cards_Revised.md."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "docs" / "card_inventory_data.json"
REVISED = ROOT / "Cards_Revised.md"
OUT = ROOT / "docs" / "IMPLEMENTED_CARDS.md"


def parse_revised(path: Path) -> dict[str, str]:
    if not path.is_file():
        return {}
    out: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if "->" not in line or line.startswith("#"):
            continue
        left, right = line.split("->", 1)
        key = re.sub(r"\s+", " ", left.strip().lower())
        out[key] = right.strip()
    return out


def class_to_revised_key(class_name: str) -> str:
    s = class_name.replace("_", " ").lower()
    return re.sub(r"\s+", " ", s.strip())


def main() -> int:
    data = json.loads(DATA.read_text(encoding="utf-8"))
    revised = parse_revised(REVISED)

    lines: list[str] = []
    lines.append("# YgoDuelist — implemented card inventory (generated)")
    lines.append("")
    lines.append("**Do not edit by hand.** Regenerate with:")
    lines.append("")
    lines.append("```text")
    lines.append("python tools/card_inventory_scan.py")
    lines.append("python tools/generate_card_inventory_md.py")
    lines.append("```")
    lines.append("")
    lines.append("## Summary counts")
    lines.append("")
    c = data["counts"]
    for k, v in c.items():
        lines.append(f"- **{k}**: {v}")
    lines.append("")
    lines.append("**Fusion monsters with C# logic beyond materials:** "
                 f"{len(data['fusionWithExtraLogic'])} (see `fusionWithExtraLogic` in `card_inventory_data.json`).")
    lines.append("")
    lines.append("**Ritual monsters with C# logic beyond empty ritual frame:** "
                 f"{len(data['ritualWithExtraLogic'])} (see `ritualWithExtraLogic`).")
    lines.append("")

    by_kind: dict[str, list] = {}
    for row in data["implemented"]:
        by_kind.setdefault(row["kind"], []).append(row)
    for kind in sorted(by_kind.keys()):
        rows = sorted(by_kind[kind], key=lambda r: r["className"])
        lines.append(f"## {kind} ({len(rows)})")
        lines.append("")
        lines.append("| Class | File | Revised effect (Cards_Revised.md) |")
        lines.append("|-------|------|-----------------------------------|")
        for row in rows:
            ck = class_to_revised_key(row["className"])
            rev = revised.get(ck, "")
            if not rev:
                for k, v in revised.items():
                    if ck in k or k in ck:
                        rev = v
                        break
            rev_cell = rev.replace("|", "\\|") if rev else "—"
            lines.append(f"| `{row['className']}` | `{row['file']}` | {rev_cell} |")
        lines.append("")

    lines.append("## Stub / template-only cards (constructor-only)")
    lines.append("")
    lines.append(f"Total: **{len(data['stubs'])}** — listed in `card_inventory_data.json` under `stubs`.")
    lines.append("")

    lines.append("## Fusion / Ritual stub appendix (materials-only or empty ritual body)")
    lines.append("")
    stub_fusion = [r for r in data["stubs"] if r["kind"] == "FusionMonster"]
    stub_ritual = [r for r in data["stubs"] if r["kind"] == "RitualMonster"]
    lines.append(f"- **FusionMonster stubs (materials in C# only):** {len(stub_fusion)}")
    lines.append(f"- **RitualMonster stubs:** {len(stub_ritual)}")
    lines.append("")

    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"Wrote {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
