"""
Fill FusionMonsterCard base() calls with fusionMaterialTypes from cards_database.json.

Run from repo root: python tools/patch_fusion_monster_materials.py --write
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "cards_database.json"
MONSTER_ROOT = ROOT / "YgoDuelistCode" / "Cards" / "Monster"
FUSION_DIR = MONSTER_ROOT / "Todo" / "Fusion"

NS_RE = re.compile(r"^namespace\s+([\w.]+)\s*;", re.M)
MAT_CLASS_RE = re.compile(
    r"public\s+sealed\s+class\s+(\w+)\s*:\s*(NormalMonsterCard|EffectMonsterCard|FusionMonsterCard|RitualMonsterCard)"
)
FUSION_CLASS_RE = re.compile(r"public\s+sealed\s+class\s+(\w+)\s*:\s*FusionMonsterCard")


def norm_key(s: str) -> str:
    s = s.lower().strip()
    s = re.sub(r"[^a-z0-9]+", "_", s)
    return s.strip("_")


def class_lookup_keys(cls: str) -> list[str]:
    keys = [norm_key(cls)]
    if cls.startswith("Card_"):
        keys.append(norm_key(cls[5:]))
    return list(dict.fromkeys(keys))


def build_db_stem_index(data: list[dict]) -> dict[str, dict]:
    idx: dict[str, dict] = {}
    for c in data:
        fn = c.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        idx[norm_key(stem)] = c
    return idx


def find_card(cls: str, idx: dict[str, dict]) -> dict | None:
    for k in class_lookup_keys(cls):
        if k in idx:
            return idx[k]
    return None


def parse_fusion_material_names(desc: str) -> list[str]:
    text = desc.replace("\r\n", "\n")
    for line in text.split("\n"):
        line = line.strip()
        if not line:
            continue
        names = re.findall(r'"([^"]*)"', line)
        if names:
            return names
    return []


def scan_material_types() -> dict[str, tuple[str, str]]:
    """norm_key -> (namespace, class_name)"""
    out: dict[str, tuple[str, str]] = {}
    for path in MONSTER_ROOT.rglob("*.cs"):
        if path.name.endswith(".uid"):
            continue
        text = path.read_text(encoding="utf-8")
        m = MAT_CLASS_RE.search(text)
        if not m:
            continue
        ns_m = NS_RE.search(text)
        if not ns_m:
            continue
        cls = m.group(1)
        nk = norm_key(cls)
        if nk in out and out[nk] != (ns_m.group(1), cls):
            raise SystemExit(f"Duplicate norm_key {nk!r}: {out[nk]} vs {(ns_m.group(1), cls)}")
        out[nk] = (ns_m.group(1), cls)
    return out


def global_type(ns: str, cls: str) -> str:
    return f"typeof(global::{ns}.{cls})"


def patch_fusion_file(path: Path, material_types: list[str], write: bool) -> None:
    text = path.read_text(encoding="utf-8")
    if re.search(r"duelMonsterRace:[^\n]+\n\s*typeof\(global::", text):
        return
    if "duelMonsterRace: DuelMonsterRace." not in text:
        raise SystemExit(f"{path}: expected duelMonsterRace in base()")

    insertion = "\n            " + ",\n            ".join(material_types)

    new_text, n = re.subn(
        r"(\n\s*duelMonsterRace:\s*DuelMonsterRace\.\w+)\)",
        rf"\1,{insertion})",
        text,
        count=1,
    )
    if n != 1:
        raise SystemExit(f"{path}: could not splice fusionMaterialTypes (matches={n})")
    if write:
        path.write_text(new_text, encoding="utf-8", newline="\n")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true")
    args = ap.parse_args()

    mat_map = scan_material_types()
    with open(DB_PATH, encoding="utf-8") as f:
        db = json.load(f)
    idx = build_db_stem_index(db)

    missing_db: list[str] = []
    missing_mat: list[tuple[str, str]] = []
    no_names: list[str] = []

    for path in sorted(FUSION_DIR.glob("*.cs")):
        if path.suffix != ".cs":
            continue
        text = path.read_text(encoding="utf-8")
        m = FUSION_CLASS_RE.search(text)
        if not m:
            continue
        cls = m.group(1)
        card = find_card(cls, idx)
        if not card or card.get("type") != "Fusion Monster":
            missing_db.append(cls)
            continue
        names = parse_fusion_material_names(card.get("desc") or "")
        if not names:
            no_names.append(cls)
            continue
        type_exprs: list[str] = []
        for mat_name in names:
            nk = norm_key(mat_name)
            hit = mat_map.get(nk)
            if not hit:
                missing_mat.append((cls, mat_name))
                continue
            type_exprs.append(global_type(hit[0], hit[1]))
        if len(type_exprs) != len(names):
            continue
        patch_fusion_file(path, type_exprs, args.write)
        print("ok", cls, len(names), "materials")

    if missing_db:
        print("MISSING DB Fusion Monster row:", missing_db)
    if no_names:
        print("NO MATERIAL LINE in desc:", no_names)
    if missing_mat:
        print("UNRESOLVED material class:", missing_mat)
    if not args.write:
        print("Dry run only. Use --write to save.")


if __name__ == "__main__":
    main()
