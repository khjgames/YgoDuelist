"""
Rewrite Fusion Monster C# files under Monster/Done/Fusion: stats from cards_database.json and
FusionMaterialTypes from the fusion recipe line (quoted names + \"+\"), same pattern as Black_Skull_Dragon.

Resolves material card names via the database (official name -> image stem -> class name) and
locates each class namespace by scanning YgoDuelistCode/Cards/**/*.cs.

Recipes that are not a list of quoted names (e.g. Five-Headed Dragon: \"5 Dragon monsters\") emit
no material types; Fusion Summon stays impossible until handled manually.

Run from repo root:
  python tools/sync_fusion_materials_from_db.py --dry-run
  python tools/sync_fusion_materials_from_db.py --write
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "cards_database.json"
CARDS_CS_ROOT = ROOT / "YgoDuelistCode" / "Cards"

import sys

sys.path.insert(0, str(ROOT / "tools"))
from generate_missing_cards_from_db import (  # noqa: E402
    map_attr,
    map_race,
    norm_key,
    scale_stat,
    stem_to_class_name,
)

MONSTER_BASES = frozenset(
    {"NormalMonsterCard", "EffectMonsterCard", "FusionMonsterCard", "RitualMonsterCard"}
)


def normalize_name(s: str) -> str:
    s = s.strip().lower()
    s = re.sub(r"\s+", " ", s)
    return s


def fusion_recipe_first_line(desc: str) -> str:
    if not desc:
        return ""
    line = desc.replace("\r\n", "\n").split("\n", 1)[0].strip()
    return line


def parse_quoted_fusion_materials(desc: str) -> list[str] | None:
    line = fusion_recipe_first_line(desc)
    if "+" not in line:
        return None
    parts = re.findall(r'"([^"]+)"', line)
    if len(parts) < 2:
        return None
    return parts


def load_name_to_image_stem(data: list[dict]) -> dict[str, str]:
    """Official card name (normalized) -> image filename stem (e.g. gaia_the_fierce_knight)."""
    m: dict[str, str] = {}
    for card in data:
        ctype = card.get("type") or ""
        if ctype in ("Spell Card", "Trap Card"):
            continue
        fn = card.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        name = card.get("name")
        if not name:
            continue
        m[normalize_name(name)] = stem
    return m


def scan_stem_norm_to_monster_class() -> dict[str, tuple[str, str]]:
    """
    norm_key(.cs file stem) and norm_key(sealed class name) -> (class_name, namespace).
    File stem matches disk layout; class name inside the file is authoritative.
    """
    out: dict[str, tuple[str, str]] = {}
    for path in CARDS_CS_ROOT.rglob("*.cs"):
        if "Command" in path.parts:
            continue
        if "Monster" not in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        ns_m = re.search(r"^namespace\s+([\w.]+);", text, re.MULTILINE)
        cm = re.search(r"public\s+sealed\s+class\s+(\w+)\s*:\s*(\w+)", text)
        if not ns_m or not cm or cm.group(2) not in MONSTER_BASES:
            continue
        cls_name = cm.group(1)
        ns = ns_m.group(1)
        out[norm_key(path.stem)] = (cls_name, ns)
        out[norm_key(cls_name)] = (cls_name, ns)
    return out


def scan_class_to_namespace() -> dict[str, str]:
    """Map C# class name -> full namespace (last file wins if duplicated)."""
    out: dict[str, str] = {}
    for path in CARDS_CS_ROOT.rglob("*.cs"):
        if "Command" in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        ns_m = re.search(r"^namespace\s+([\w.]+);", text, re.MULTILINE)
        if not ns_m:
            continue
        for cm in re.finditer(r"public\s+sealed\s+class\s+(\w+)\s*:\s*(\w+)", text):
            base = cm.group(2)
            if base in MONSTER_BASES:
                out[cm.group(1)] = ns_m.group(1)
    return out


def resolve_material_type(
    raw: str,
    name_to_stem: dict[str, str],
    stem_norm_to_class: dict[str, tuple[str, str]],
    cls_to_ns: dict[str, str],
) -> str:
    key = normalize_name(raw)
    stem = name_to_stem.get(key)
    if not stem:
        raise KeyError(f"Material name not in database as a monster: {raw!r} (normalized {key!r})")
    sk = norm_key(stem)
    if sk in stem_norm_to_class:
        cls_name, ns = stem_norm_to_class[sk]
    else:
        cls_name = stem_to_class_name(stem)
        ns = cls_to_ns.get(cls_name)
        if not ns:
            raise KeyError(f"No C# monster class found for material {raw!r} (stem {stem!r} -> {cls_name})")
    return f"typeof(global::{ns}.{cls_name})"


def material_typeof_exprs(
    material_names: list[str],
    name_to_stem: dict[str, str],
    stem_norm_to_class: dict[str, tuple[str, str]],
    cls_to_ns: dict[str, str],
) -> list[str]:
    return [resolve_material_type(n, name_to_stem, stem_norm_to_class, cls_to_ns) for n in material_names]


def render_fusion_class(
    cls: str,
    level: int,
    attr: str,
    race: str,
    atk: int,
    deff: int,
    material_exprs: list[str],
    unfusable_comment: str | None,
) -> str:
    comment = f"    // {unfusable_comment}\n" if unfusable_comment else ""
    if material_exprs:
        mats = ",\n            ".join(material_exprs)
        tail = f",\n            {mats})"
    else:
        tail = ")"
    return f"""using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class {cls} : FusionMonsterCard
{{
{comment}    public {cls}()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: {level},
            duelMonsterAttribute: DuelMonsterAttribute.{attr},
            baseAtk: {atk},
            baseDef: {deff},
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.{race}{tail}
    {{
    }}
}}
"""


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    with open(DB_PATH, "r", encoding="utf-8") as f:
        data: list[dict] = json.load(f)

    name_to_stem = load_name_to_image_stem(data)
    stem_norm_to_class = scan_stem_norm_to_monster_class()
    cls_to_ns = scan_class_to_namespace()

    fusion_dir = CARDS_CS_ROOT / "Monster" / "Done" / "Fusion"
    updated = 0
    skipped = 0
    errors: list[str] = []

    for card in data:
        if card.get("type") != "Fusion Monster":
            continue
        fn = card.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        cls = stem_to_class_name(stem)
        path = fusion_dir / f"{cls}.cs"
        if not path.exists():
            skipped += 1
            continue

        desc = card.get("desc") or ""
        parsed = parse_quoted_fusion_materials(desc)
        unfusable: str | None = None
        material_exprs: list[str] = []
        if parsed is None:
            unfusable = (
                f"DB fusion recipe not parsed as quoted names: {fusion_recipe_first_line(desc)!r} — no FusionMaterialTypes."
            )
        else:
            try:
                material_exprs = material_typeof_exprs(parsed, name_to_stem, stem_norm_to_class, cls_to_ns)
            except KeyError as e:
                errors.append(f"{cls}: {e}")
                continue

        level = int(card.get("level") or 1)
        attr = map_attr(card.get("attribute"))
        race = map_race(card.get("race"))
        atk = scale_stat(card.get("atk"))
        deff = scale_stat(card.get("def"))

        body = render_fusion_class(cls, level, attr, race, atk, deff, material_exprs, unfusable)
        if path.read_text(encoding="utf-8") == body:
            continue
        updated += 1
        print("update", path.relative_to(ROOT))
        if args.write:
            path.write_text(body, encoding="utf-8")

    print("Updated:", updated, "Skipped (no file):", skipped)
    if errors:
        print("Errors:")
        for e in errors:
            print(" ", e)
        raise SystemExit(1)
    if not args.write and not args.dry_run and updated:
        print("Use --write to apply.")


if __name__ == "__main__":
    main()
