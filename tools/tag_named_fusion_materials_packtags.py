# Prepend YgoCardPackTags.MultiplayerSafe to PackTags for every *named* fusion material
# (types that appear as FusionMaterialSlot.NamedType in a FusionMonsterCard : base recipe).
# Matches runtime FusionMaterialArchetypeIndex without Activator; does not add tags for
# requirement-only recipe slots (e.g. Five-Headed Dragon).

from __future__ import annotations

import re
import sys
from pathlib import Path

PREFIX = "YgoDuelist.YgoDuelistCode.Cards."
GLOBAL_PREFIX = "global::YgoDuelist.YgoDuelistCode.Cards."


def extract_base_args_inner(text: str) -> str:
    m = re.search(r":\s*base\s*\(", text)
    if not m:
        return ""
    i = m.end() - 1
    depth = 1
    start = i + 1
    i += 1
    n = len(text)
    while i < n and depth > 0:
        c = text[i]
        if c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
            if depth == 0:
                return text[start:i]
        elif c == '"' or c == "'":
            q = c
            i += 1
            while i < n:
                if text[i] == "\\":
                    i += 2
                    continue
                if text[i] == q:
                    break
                i += 1
        i += 1
    return ""


def build_class_index(cards: Path) -> dict[str, Path]:
    out: dict[str, Path] = {}
    for p in cards.glob("Monster/**/*.cs"):
        try:
            t = p.read_text(encoding="utf-8")
        except OSError:
            continue
        m = re.search(r"public\s+(?:sealed\s+)?class\s+(\w+)\s*[:\{]", t)
        if m:
            out[m.group(1)] = p
    return out


def resolve_typeof_arg(arg: str, cards: Path, class_index: dict[str, Path]) -> Path | None:
    arg = arg.strip()
    if arg.startswith(GLOBAL_PREFIX):
        tail = arg[len(GLOBAL_PREFIX) :]
        rel = tail.replace(".", "/") + ".cs"
        p = cards / rel
        return p if p.is_file() else None
    if arg.startswith(PREFIX):
        tail = arg[len(PREFIX) :]
        rel = tail.replace(".", "/") + ".cs"
        p = cards / rel
        return p if p.is_file() else None
    simple = re.match(r"^(\w+)$", arg)
    if simple:
        name = simple.group(1)
        return class_index.get(name)
    return None


def ensure_multiline_safe_first(text: str) -> str | None:
    if "YgoCardPackTags.MultiplayerSafe" in text and re.search(
        r"PackTags\s*=>\s*YgoCardPackTags\.MultiplayerSafe", text
    ):
        return None
    new = re.sub(
        r"(public\s+override\s+YgoCardPackTags\s+PackTags\s*=>\s*)(?!YgoCardPackTags\.MultiplayerSafe)",
        r"\1YgoCardPackTags.MultiplayerSafe | ",
        text,
        count=1,
    )
    return new if new != text else None


def inherits_fusion_monster_no_packtags_override(text: str) -> bool:
    if ": FusionMonsterCard" not in text:
        return False
    return re.search(r"override\s+YgoCardPackTags\s+PackTags", text) is None


def collect_material_paths(fusion_dir: Path, cards: Path, class_index: dict[str, Path]) -> set[Path]:
    materials: set[Path] = set()
    for fusion_file in sorted(fusion_dir.glob("*.cs")):
        body = fusion_file.read_text(encoding="utf-8")
        inner = extract_base_args_inner(body)
        if not inner:
            continue
        for m in re.finditer(r"typeof\s*\(\s*([^)]+)\s*\)", inner):
            p = resolve_typeof_arg(m.group(1), cards, class_index)
            if p is not None:
                materials.add(p.resolve())
    return materials


def main() -> int:
    repo = Path(__file__).resolve().parents[1]
    cards = repo / "YgoDuelistCode" / "Cards"
    fusion_dir = cards / "Monster" / "Done" / "Fusion"
    class_index = build_class_index(cards)
    mats = collect_material_paths(fusion_dir, cards, class_index)
    updated = 0
    missing = 0
    for path in sorted(mats):
        text = path.read_text(encoding="utf-8")
        new_text = ensure_multiline_safe_first(text)
        if new_text is None:
            if "YgoCardPackTags.MultiplayerSafe" in text:
                continue
            if inherits_fusion_monster_no_packtags_override(text):
                continue
            print(f"SKIP could not patch PackTags: {path.relative_to(repo)}", file=sys.stderr)
            missing += 1
            continue
        path.write_text(new_text, encoding="utf-8", newline="\n")
        updated += 1
    print(f"Prepended MultiplayerSafe on PackTags: {updated} files ({len(mats)} named materials resolved)")
    if missing:
        print(f"Unresolved (manual PackTags pattern?): {missing}", file=sys.stderr)
    return 0 if missing == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
