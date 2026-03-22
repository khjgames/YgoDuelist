"""
Map monster C# classes to cards_database.json rows via normalized image_filename stem.
Run: python tools/sync_normal_monster_locales.py
  --dry-run   print counts only
  --write     patch YgoDuelist/localization/eng/cards.json
  --skip-normals-and-fusions   only apply normal Ritual Monster strings (DB type exactly Ritual Monster)

Default run updates: Normal Monster, vanilla Fusion Monster, and normal Ritual Monster entries.
Ritual Effect Monster / wrong DB type are skipped for the ritual template.
"""
from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "cards_database.json"
NORMAL_DIR = ROOT / "YgoDuelistCode" / "Cards" / "Monster" / "Todo" / "Normal"
FUSION_DIR = ROOT / "YgoDuelistCode" / "Cards" / "Monster" / "Todo" / "Fusion"
RITUAL_DIR = ROOT / "YgoDuelistCode" / "Cards" / "Monster" / "Todo" / "Ritual"
CARDS_JSON = ROOT / "YgoDuelist" / "localization" / "eng" / "cards.json"

MAT_LINE = re.compile(r'^(\s*"[^"]+"(\s*\+\s*"[^"]+")*\s*)$')
CLASS_RE = re.compile(r"public sealed class (\w+) : (NormalMonsterCard|FusionMonsterCard)")


def norm_key(s: str) -> str:
    s = s.lower().strip()
    s = re.sub(r"[^a-z0-9]+", "_", s)
    return s.strip("_")


def is_normal_fusion_desc(desc: str) -> bool:
    if not desc:
        return False
    lines = [l.strip() for l in desc.replace("\r\n", "\n").split("\n") if l.strip()]
    if not lines:
        return False
    if not MAT_LINE.match(lines[0]):
        return False
    for ln in lines[1:]:
        if ln.startswith("(") and "This card" in ln:
            continue
        if ln.startswith("(") and "Fusion Material" in ln:
            continue
        if ln.startswith("(") and len(ln) < 120:
            continue
        return False
    return True


def parse_materials(first_line: str) -> list[str]:
    return re.findall(r'"([^"]+)"', first_line.split("\n")[0].strip())


RITUAL_SPELL_PATTERNS = (
    re.compile(r'You can Ritual Summon this card with "([^"]+)"'),
    re.compile(r'Ritual Spell Card, "([^"]+)"'),
)


def extract_ritual_spell_name(desc: str) -> str | None:
    """First quoted ritual spell name from card text (DB ritual monster desc)."""
    text = desc.replace("\r\n", "\n")
    for pat in RITUAL_SPELL_PATTERNS:
        m = pat.search(text)
        if m:
            return m.group(1).strip()
    return None


def ritual_summon_line(spell_name: str) -> str:
    return f'Can be summoned with "{spell_name}".'


def fusion_summon_line(names: list[str]) -> str:
    if not names:
        return ""
    counts = Counter(names)
    ordered: list[tuple[str, int]] = []
    seen: set[str] = set()
    for n in names:
        if n not in seen:
            seen.add(n)
            ordered.append((n, counts[n]))
    parts: list[str] = []
    for n, c in ordered:
        parts.append(f'{c} "{n}"' if c != 1 else f'"{n}"')
    if len(parts) == 1:
        inner = parts[0]
    elif len(parts) == 2:
        inner = f"{parts[0]} and {parts[1]}"
    else:
        inner = ", ".join(parts[:-1]) + f", and {parts[-1]}"
    return f"Can be summoned with {inner} as materials."


RITUAL_CLASS_RE = re.compile(r"public sealed class (\w+) : RitualMonsterCard")


def load_classes(folder: Path, kind: str) -> list[str]:
    out: list[str] = []
    for p in sorted(folder.glob("*.cs")):
        m = CLASS_RE.search(p.read_text(encoding="utf-8"))
        if m and m.group(2) == kind:
            out.append(m.group(1))
    return out


def load_ritual_classes(folder: Path) -> list[str]:
    out: list[str] = []
    for p in sorted(folder.glob("*.cs")):
        m = RITUAL_CLASS_RE.search(p.read_text(encoding="utf-8"))
        if m:
            out.append(m.group(1))
    return out


def entry_from_class(cls: str) -> str:
    return "YGODUELIST-" + cls.upper()


def build_db_stem_index(data: list[dict]) -> dict[str, dict]:
    idx: dict[str, dict] = {}
    for c in data:
        fn = c.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        k = norm_key(stem)
        idx[k] = c
    return idx


def class_lookup_keys(cls: str) -> list[str]:
    keys = [norm_key(cls)]
    if cls.startswith("Card_"):
        keys.append(norm_key(cls[5:]))
    return list(dict.fromkeys(keys))


def find_card(cls: str, idx: dict[str, dict]) -> dict | None:
    for k in class_lookup_keys(cls):
        if k in idx:
            return idx[k]
    return None


def lines_for_normal(entry: str, title: str) -> dict[str, str]:
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


def lines_for_normal_ritual(entry: str, title: str, mid_line: str) -> dict[str, str]:
    base = entry.removeprefix("YGODUELIST-")
    mid = f"\n{mid_line}\n"
    return {
        f"YGODUELIST-{base}.title": title,
        f"YGODUELIST-{base}.description": (
            "Deal [blue]{Damage}[/blue] damage." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]"
        ),
        f"YGODUELIST-{base}.description_skill": (
            "Gain [blue]{Block}[/blue] block." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]"
        ),
        f"YGODUELIST-{base}.description_combat": (
            "Deal [blue]{CalculatedATK}[/blue] damage." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]"
        ),
        f"YGODUELIST-{base}.description_skill_combat": (
            "Gain [blue]{CalculatedDEF}[/blue] block." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]"
        ),
    }


def lines_for_fusion(entry: str, title: str, material_line: str) -> dict[str, str]:
    base = entry.removeprefix("YGODUELIST-")
    mid = f"\n{material_line}\n"
    return {
        f"YGODUELIST-{base}.title": title,
        f"YGODUELIST-{base}.description": (
            "Deal [blue]{Damage}[/blue] damage." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]"
        ),
        f"YGODUELIST-{base}.description_skill": (
            "Gain [blue]{Block}[/blue] block." + mid + "ATK [blue]{Damage}[/blue] / DEF [blue]{Def}[/blue]"
        ),
        f"YGODUELIST-{base}.description_combat": (
            "Deal [blue]{CalculatedATK}[/blue] damage." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]"
        ),
        f"YGODUELIST-{base}.description_skill_combat": (
            "Gain [blue]{CalculatedDEF}[/blue] block." + mid + "ATK [blue]{CalculatedATK}[/blue] / DEF [blue]{CalculatedDEF}[/blue]"
        ),
    }


def patch_cards_json(path: Path, updates: dict[str, str]) -> None:
    with open(path, "r", encoding="utf-8") as f:
        data: dict[str, str] = json.load(f)
    for k, v in updates.items():
        data[k] = v
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False, sort_keys=True)
        f.write("\n")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--write", action="store_true")
    ap.add_argument(
        "--skip-normals-and-fusions",
        action="store_true",
        help="Only update normal Ritual Monsters (DB type exactly 'Ritual Monster').",
    )
    args = ap.parse_args()

    with open(DB_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)

    idx = build_db_stem_index(data)

    updates: dict[str, str] = {}
    miss_n: list[str] = []
    wrong_n: list[tuple[str, str]] = []
    miss_f: list[str] = []
    wrong_f: list[tuple[str, str]] = []
    skip_effect: list[str] = []

    if not args.skip_normals_and_fusions:
        normals = load_classes(NORMAL_DIR, "NormalMonsterCard")
        fusions = load_classes(FUSION_DIR, "FusionMonsterCard")

        for cls in normals:
            card = find_card(cls, idx)
            ent = entry_from_class(cls)
            if not card:
                miss_n.append(cls)
                continue
            if card.get("type") != "Normal Monster":
                wrong_n.append((cls, card.get("type", "")))
                continue
            updates.update(lines_for_normal(ent, card["name"]))

        for cls in fusions:
            card = find_card(cls, idx)
            ent = entry_from_class(cls)
            if not card:
                miss_f.append(cls)
                continue
            if card.get("type") != "Fusion Monster":
                wrong_f.append((cls, card.get("type", "")))
                continue
            desc = card.get("desc") or ""
            if not is_normal_fusion_desc(desc):
                skip_effect.append(cls)
                continue
            first = desc.replace("\r\n", "\n").split("\n")
            first_line = next((l.strip() for l in first if l.strip()), "")
            mats = parse_materials(first_line)
            if not mats:
                skip_effect.append(cls)
                continue
            updates.update(lines_for_fusion(ent, card["name"], fusion_summon_line(mats)))
    else:
        normals = []
        fusions = []

    rituals = load_ritual_classes(RITUAL_DIR)
    miss_r: list[str] = []
    wrong_r: list[tuple[str, str]] = []
    skip_r_effect: list[str] = []
    no_spell: list[str] = []

    for cls in rituals:
        card = find_card(cls, idx)
        ent = entry_from_class(cls)
        if not card:
            miss_r.append(cls)
            continue
        if card.get("type") != "Ritual Monster":
            skip_r_effect.append(cls)
            continue
        spell = extract_ritual_spell_name(card.get("desc") or "")
        if not spell:
            no_spell.append(cls)
            continue
        updates.update(lines_for_normal_ritual(ent, card["name"], ritual_summon_line(spell)))

    if not args.skip_normals_and_fusions:
        print("NormalMonsterCard:", len(normals), "mapped:", len(normals) - len(miss_n) - len(wrong_n))
        print("  missing image match:", len(miss_n), miss_n[:8])
        print("  wrong DB type:", wrong_n[:8])
        print("FusionMonsterCard:", len(fusions), "normal fusion mapped:", len(fusions) - len(miss_f) - len(wrong_f) - len(skip_effect))
        print("  missing:", len(miss_f), "wrong type:", len(wrong_f), "effect fusion skipped:", len(skip_effect))
    print("RitualMonsterCard (DB type Ritual Monster only):", len(rituals), "mapped:", len(rituals) - len(miss_r) - len(skip_r_effect) - len(no_spell))
    print("  missing image match:", len(miss_r), miss_r[:8])
    print("  skipped (not normal ritual / wrong type):", len(skip_r_effect), skip_r_effect)
    print("  no ritual spell in desc:", no_spell)
    print("Total localization keys to set:", len(updates))

    if args.write:
        patch_cards_json(CARDS_JSON, updates)
        print("Wrote", CARDS_JSON)
    elif not args.dry_run and not args.write:
        print("Use --write to apply, or --dry-run to suppress this hint.")


if __name__ == "__main__":
    main()
