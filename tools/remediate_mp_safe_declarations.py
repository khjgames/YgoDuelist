# Strip bulk-added MultiplayerSafe from declared PackTags when not allowed by narrow rules.
# See Game_Design/Multiplayer_Safe_First_Pass.md (keep in sync after edits).

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

THEME_RE = re.compile(
    r"YgoCardPackTags\.(Draw|Chance|Banish|WinCon|God|Heal)\b"
)

# Paths relative to YgoDuelistCode/Cards/
STAPLE_RELS = {
    "Spell/Monster_Reborn.cs",
    "Spell/Done/Normal/Double_Summon.cs",
    "Trap/Done/Linked/Call_of_the_Haunted.cs",
    "Spell/Done/Field/Fusion_Gate.cs",
    "Monster/Done/Effect/Lava_Golem.cs",
    "Spell/Done/Equip/Mask_of_Brutality.cs",
    "Spell/Done/Equip/Mask_of_the_Burdened.cs",
    "Trap/Done/Linked/Mask_of_Weakness.cs",
    "Spell/Done/Normal/Polymerization.cs",
    "Trap/Done/Linked/The_First_Monarch.cs",
}


def parse_explicit_picks(repo_root: Path) -> set[str]:
    doc = repo_root / "Game_Design" / "Multiplayer_Safe_First_Pass.md"
    text = doc.read_text(encoding="utf-8")
    try:
        start = text.index("## Former explicit pick list")
    except ValueError:
        return set()
    chunk = text[start : start + 12000]
    out: set[str] = set()
    for m in re.finditer(r"`(YgoDuelist\.YgoDuelistCode\.Cards\.[^`]+)`", chunk):
        fq = m.group(1)
        tail = fq.removeprefix("YgoDuelist.YgoDuelistCode.Cards.")
        parts = tail.split(".")
        if len(parts) < 2:
            continue
        classname = parts[-1]
        dir_parts = parts[:-1]
        rel = "/".join(dir_parts) + f"/{classname}.cs"
        out.add(rel.replace("\\", "/"))
    return out


def posix_under_cards(p: Path, cards: Path) -> str:
    try:
        rel = p.relative_to(cards).as_posix()
    except ValueError:
        return ""
    return rel


def should_keep_mp(rel_under_cards: str, text: str, explicit: set[str]) -> bool:
    """rel_under_cards uses forward slashes from Cards/."""
    reln = rel_under_cards.replace("\\", "/")
    if reln in explicit:
        return True
    if reln in STAPLE_RELS:
        return True

    rel = rel_under_cards.replace("\\", "/")

    if "/Monster/Done/Ritual/" in rel or "/Spell/Done/Ritual/" in rel:
        return True
    if "/Monster/Elemental/" in rel:
        return True
    if "/Monster/Done/TrapMonster/" in rel:
        return True
    if "/Spell/Done/Equip/" in rel:
        return True
    if "/Spell/Done/Field/" in rel:
        return True
    if "/Monster/Done/Fusion/" in rel:
        return True
    if "/Monster/Done/Token/" in rel:
        return True

    if ", IDoubleTributeMaterial" in text:
        return True

    m_class = re.search(r"public\s+(?:sealed\s+)?class\s+(\w+)", text)
    cname = m_class.group(1) if m_class else ""
    if "Gravekeeper" in cname or "Gravekeeper" in Path(rel).name:
        return True
    if "Monarch" in cname:
        return True

    if THEME_RE.search(text):
        return True

    return False


def strip_multplayer_safe_flags(text: str) -> str:
    """Remove MultiplayerSafe from bitwise PackTags chains only (typical patterns)."""
    s = text
    # Trailing: ... | MultiplayerSafe
    s = re.sub(r"\s*\|\s*YgoCardPackTags\.MultiplayerSafe\b", "", s)
    # Leading: MultiplayerSafe | ...
    s = re.sub(r"\bYgoCardPackTags\.MultiplayerSafe\s*\|\s*", "", s)
    return s


def iter_card_cs_files(cards_root: Path) -> list[Path]:
    roots = [
        cards_root / "Monster" / "Done" / "Effect",
        cards_root / "Spell",
        cards_root / "Trap",
        cards_root / "Monster" / "Done" / "Token",
    ]
    out: list[Path] = []
    for r in roots:
        if not r.exists():
            continue
        out.extend(p for p in r.rglob("*.cs") if p.is_file())
    return sorted(out)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    repo = Path(__file__).resolve().parents[1]
    cards = repo / "YgoDuelistCode" / "Cards"
    explicit = parse_explicit_picks(repo)

    stripped = 0
    skipped = 0
    for path in iter_card_cs_files(cards):
        rel = posix_under_cards(path, cards)
        if not rel:
            continue
        text = path.read_text(encoding="utf-8")
        if "MultiplayerSafe" not in text:
            continue
        if not should_keep_mp(rel, text, explicit):
            new_text = strip_multplayer_safe_flags(text)
            if new_text == text:
                print(f"WARN: no change possible {path}", file=sys.stderr)
                continue
            if "MultiplayerSafe" in new_text:
                print(f"WARN: still contains MultiplayerSafe after strip {path}", file=sys.stderr)
            if args.dry_run:
                print(f"would strip: {rel}")
                stripped += 1
            else:
                path.write_text(new_text, encoding="utf-8", newline="\n")
                stripped += 1
        else:
            skipped += 1

    print(f"{'Would strip' if args.dry_run else 'Stripped'}: {stripped} files; kept as allowed: {skipped} files with MultiplayerSafe")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
