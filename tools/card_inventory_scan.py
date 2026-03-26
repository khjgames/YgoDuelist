"""
Scan YgoDuelistCode/Cards for concrete card classes, excluding Normal monsters
and constructor-only stubs. Outputs JSON for markdown generation.
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from dataclasses import dataclass, asdict
from typing import List, Optional

CARDS_ROOT = Path(__file__).resolve().parent.parent / "YgoDuelistCode" / "Cards"

# Strip block and line comments (approximate for C#)
def strip_comments(text: str) -> str:
    out = []
    i = 0
    n = len(text)
    while i < n:
        if i + 1 < n and text[i : i + 2] == "//":
            while i < n and text[i] != "\n":
                i += 1
            continue
        if i + 1 < n and text[i : i + 2] == "/*":
            end = text.find("*/", i + 2)
            if end == -1:
                break
            i = end + 2
            continue
        out.append(text[i])
        i += 1
    return "".join(out)


# Base clause may span lines before the class-opening `{`
CLASS_RE = re.compile(
    r"public\s+sealed\s+class\s+(\w+)\s*:\s*([^{]+?)\s*\{",
    re.DOTALL,
)

# Members that indicate non-template implementation
IMPL_PATTERNS = [
    re.compile(r"\boverride\b"),
    re.compile(r"\bnew\s+DynamicVar\b"),
    re.compile(r"\bCanonicalVars\b"),
    re.compile(r"\bGetStatEffect\b"),
    re.compile(r"\bGetSecondaryStats\b"),
    re.compile(r"\bOnUpgrade\b"),
    re.compile(r"\bOnSpellPlay\b"),
    re.compile(r"\bOnTrapPlay\b"),
    re.compile(r"\bCombatAction\b"),
    re.compile(r"\basync\s+Task\b"),
    re.compile(r",\s*IMonster"),  # flip/activate interfaces
    re.compile(r",\s*I[A-Z]\w+\s*\{"),  # interface on same line rare
]


def normalize_base(base_clause: str) -> str:
    s = base_clause.strip()
    # Take first base type before comma (interfaces)
    if "," in s:
        s = s.split(",")[0].strip()
    return s


def is_normal_monster_direct(base: str) -> bool:
    b = normalize_base(base)
    return b == "NormalMonsterCard" or b.endswith(".NormalMonsterCard")


def classify_card_kind(base: str) -> str:
    b = normalize_base(base)
    if "FusionMonsterCard" in b:
        return "FusionMonster"
    if "RitualMonsterCard" in b:
        return "RitualMonster"
    if "EffectMonsterCard" in b:
        return "EffectMonster"
    if "BaseEquipSpellCard" in b:
        return "EquipSpell"
    if "BaseFieldSpellCard" in b:
        return "FieldSpell"
    if "BaseContinuousSpellCard" in b:
        return "ContinuousSpell"
    if "FusionSpellCard" in b:
        return "FusionSpell"
    if "RitualSpellCard" in b:
        return "RitualSpell"
    if "BaseSpellCard" in b:
        return "Spell"
    if "BaseTrapCard" in b:
        return "Trap"
    return "Other"


def extract_class_body(content: str, open_brace_index: int) -> str:
    """Inner content of the outermost brace pair starting at open_brace_index."""
    depth = 0
    i = open_brace_index
    while i < len(content):
        if content[i] == "{":
            depth += 1
        elif content[i] == "}":
            depth -= 1
            if depth == 0:
                return content[open_brace_index + 1 : i]
        i += 1
    return ""


def has_implementation(body: str) -> bool:
    clean = strip_comments(body)
    for pat in IMPL_PATTERNS:
        if pat.search(clean):
            return True
    # Explicit interface implementation
    if re.search(r"\bvoid\s+I\w+\.", clean):
        return True
    return False


def scan_file(path: Path) -> List[dict]:
    text = path.read_text(encoding="utf-8")
    results = []
    for m in CLASS_RE.finditer(text):
        name, base_clause = m.group(1), m.group(2)
        if is_normal_monster_direct(base_clause):
            continue
        base = normalize_base(base_clause)
        kind = classify_card_kind(base_clause)
        # Skip abstract bases named like *Card in Core if any slipped (shouldn't be sealed)
        rel = path.relative_to(CARDS_ROOT.parent.parent)
        open_brace = m.end() - 1
        body = extract_class_body(text, open_brace)
        impl = has_implementation(body)
        fusion_extra = kind == "FusionMonster" and impl
        ritual_extra = kind == "RitualMonster" and impl
        results.append(
            {
                "className": name,
                "file": str(rel).replace("\\", "/"),
                "directBase": base,
                "kind": kind,
                "hasNonTemplateLogic": impl,
                "fusionExtraBeyondMaterials": fusion_extra,
                "ritualExtraBeyondSpell": ritual_extra,
            }
        )
    return results


def main() -> int:
    if not CARDS_ROOT.is_dir():
        print(f"Missing {CARDS_ROOT}", file=sys.stderr)
        return 1
    all_rows: List[dict] = []
    for cs in sorted(CARDS_ROOT.rglob("*.cs")):
        if cs.name.endswith(".cs.uid"):
            continue
        all_rows.extend(scan_file(cs))

    implemented = [r for r in all_rows if r["hasNonTemplateLogic"]]
    stubs = [r for r in all_rows if not r["hasNonTemplateLogic"]]
    fusion_extra = [r for r in all_rows if r["fusionExtraBeyondMaterials"]]
    ritual_extra = [r for r in all_rows if r["ritualExtraBeyondSpell"]]

    out_dir = Path(__file__).resolve().parent.parent / "docs"
    out_dir.mkdir(exist_ok=True)
    json_path = out_dir / "card_inventory_data.json"
    payload = {
        "implemented": sorted(implemented, key=lambda x: (x["kind"], x["className"])),
        "stubs": sorted(stubs, key=lambda x: (x["kind"], x["className"])),
        "fusionWithExtraLogic": fusion_extra,
        "ritualWithExtraLogic": ritual_extra,
        "counts": {
            "implemented": len(implemented),
            "stubs": len(stubs),
            "fusionExtra": len(fusion_extra),
            "ritualExtra": len(ritual_extra),
            "totalSealedClassesExcludingNormalMonsters": len(all_rows),
        },
    }
    json_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"Wrote {json_path}")
    print(json.dumps(payload["counts"], indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
