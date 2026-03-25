"""
Rename card_portraits/*.png to match YgoDuelistCard PortraitPath:
  {SealedClassName}.ToLowerInvariant() + ".png"

Handles ALL CAPS slugs, MK3 vs MK_3, KA-2, etc. Skips non-card assets (Face_Down*, card.png, …).

  python tools/sync_card_portrait_names.py
  python tools/sync_card_portrait_names.py --apply
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS_DIR = ROOT / "YgoDuelistCode" / "Cards"
PORTRAITS_DIR = ROOT / "YgoDuelist" / "images" / "card_portraits"

CLASS_RE = re.compile(
    r"^\s*public\s+(?:sealed\s+)?class\s+(\w+)\s*(?::\s*([\w.<>,\s]+))?",
    re.MULTILINE,
)
ABSTRACT_RE = re.compile(r"^\s*public\s+abstract\s+class\s+(\w+)", re.MULTILINE)

# Hardcoded in C#; keep exact spelling/casing.
SKIP_FILES = frozenset(
    {
        "Face_Down.png",
        "Face_Down_2.png",
        "Face_Down_3.png",
        "card.png",
        "card_back.png",
        "equip_portrait_overlay.png",
        "12_stars.png",
        "double_attack.png",
    }
)


def collect_concrete_card_classes() -> set[str]:
    abstract_names: set[str] = set()
    for p in CARDS_DIR.rglob("*.cs"):
        t = p.read_text(encoding="utf-8", errors="replace")
        for m in ABSTRACT_RE.finditer(t):
            abstract_names.add(m.group(1))

    concrete: set[str] = set()
    for p in CARDS_DIR.rglob("*.cs"):
        t = p.read_text(encoding="utf-8", errors="replace")
        for m in CLASS_RE.finditer(t):
            name = m.group(1)
            start = m.start()
            line_start = t.rfind("\n", 0, start) + 1
            nl = t.find("\n", start)
            line = t[line_start:nl if nl != -1 else len(t)]
            if "abstract class" in line:
                continue
            if name in abstract_names:
                continue
            concrete.add(name)
    return concrete


def norm_slug(stem: str) -> str:
    """Same idea as tools/generate_missing_cards_from_db.norm_key: stable slug."""
    s = stem.lower().strip()
    s = re.sub(r"[^a-z0-9]+", "_", s)
    s = re.sub(r"_+", "_", s).strip("_")
    return s


def acronym_fixes(slug: str) -> str:
    """Insert underscores for MK2 / KA2 style segments to match C# identifiers."""
    s = slug
    # _mk3 -> _mk_3; bugrothmk3 -> bugroth_mk_3; ^mk3 -> mk_3
    s = re.sub(r"_mk(\d+)(?=[_]|$)", r"_mk_\1", s)
    s = re.sub(r"(?<=[a-z0-9])mk(\d+)(?=[_]|$)", r"_mk_\1", s)
    s = re.sub(r"^mk(\d+)(?=[_]|$)", r"mk_\1", s)
    # ka_2_des / ka2_
    s = re.sub(r"(^|_)ka(\d+)(?=[_]|$)", r"\1ka_\2", s)
    # d_n_a -> dna
    s = re.sub(r"(^|_)d_n_a($|_)", r"\1dna\2", s)
    # u_f_o -> ufo
    s = re.sub(r"(^|_)u_f_o($|_)", r"\1ufo\2", s)
    s = re.sub(r"_+", "_", s).strip("_")
    return s


def resolve_target_name(stem: str, expected: set[str]) -> str | None:
    slug = norm_slug(stem)
    candidates = [slug]
    fixed = acronym_fixes(slug)
    if fixed != slug:
        candidates.append(fixed)
    if re.match(r"^\d", fixed):
        candidates.append("card_" + fixed)
    for c in candidates:
        name = c + ".png"
        if name in expected:
            return name
    return None


def patch_import(text: str, old: str, new: str) -> str:
    return text.replace(old, new)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    concrete = collect_concrete_card_classes()
    expected = {n.lower() + ".png" for n in concrete}

    moves: dict[Path, str] = {}
    for f in sorted(PORTRAITS_DIR.iterdir()):
        if f.suffix.lower() != ".png":
            continue
        if f.name in SKIP_FILES:
            continue
        if f.name in expected:
            continue
        tgt = resolve_target_name(f.stem, expected)
        if tgt is None or tgt == f.name:
            continue
        moves[f] = tgt

    by_target: dict[str, list[Path]] = {}
    for src, tgt in moves.items():
        by_target.setdefault(tgt, []).append(src)

    deletes: list[Path] = []
    renames: list[tuple[Path, Path]] = []
    for tgt, sources in sorted(by_target.items(), key=lambda x: x[0]):
        dest = PORTRAITS_DIR / tgt
        sources = sorted(sources, key=lambda p: p.name)
        if dest.exists() and dest not in sources:
            for s in sources:
                deletes.append(s)
            continue
        primary = sources[0]
        if primary != dest:
            renames.append((primary, dest))
        for s in sources[1:]:
            deletes.append(s)

    for f in deletes:
        print(f"DELETE dup: {f.name}")
        if args.apply:
            f.unlink(missing_ok=True)
            f.with_suffix(f.suffix + ".import").unlink(missing_ok=True)

    for src, dst in renames:
        print(f"RENAME: {src.name} -> {dst.name}")
        if args.apply:
            imp_src = src.with_suffix(src.suffix + ".import")
            src.rename(dst)
            if imp_src.is_file():
                txt = imp_src.read_text(encoding="utf-8", errors="replace")
                txt = patch_import(txt, src.name, dst.name)
                imp_dst = dst.with_suffix(dst.suffix + ".import")
                if imp_dst.is_file():
                    imp_dst.unlink()
                imp_src.rename(imp_dst)
                imp_dst.write_text(txt, encoding="utf-8")

    if not args.apply and (deletes or renames):
        print("Dry run. Pass --apply to execute.", file=sys.stderr)
    elif not deletes and not renames:
        print("Nothing to do; all portraits match card classes (or are skipped assets).", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
