# Set NormalMonsterCard PackTags MultiplayerSafe for top PERCENTILE tier + legacy explicit normals.
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import list_high_tier_true_normals as tier

# Former explicit picks under Monster/Done/Normal (always tagged regardless of percentile rank).
ALWAYS_MP_NORMALS = frozenset({
    "Summoned_Skull.cs",
    "Gigobyte.cs",
    "Millennium_Shield.cs",
    "Skull_Servant.cs",
})


def strip_mp(text: str) -> str:
    s = re.sub(r"\s*\|\s*YgoCardPackTags\.MultiplayerSafe\b", "", text)
    s = re.sub(r"\bYgoCardPackTags\.MultiplayerSafe\s*\|\s*", "", s)
    return s


def ensure_mp(text: str) -> str:
    if re.search(
        r"public override YgoCardPackTags PackTags =>\s*YgoCardPackTags\.MultiplayerSafe",
        text,
    ):
        return text
    return re.sub(
        r"(public override YgoCardPackTags PackTags =>)\s*(?!YgoCardPackTags\.MultiplayerSafe)",
        r"\1 YgoCardPackTags.MultiplayerSafe | ",
        text,
        count=1,
    )


def main() -> None:
    repo = Path(__file__).resolve().parents[1]
    root = repo / "YgoDuelistCode" / "Cards" / "Monster" / "Done" / "Normal"
    rows = tier.collect_scored_normals(root)
    rows.sort(key=lambda x: x[1], reverse=True)
    n = len(rows)
    k = max(1, math.ceil(n * tier.PERCENTILE_DEFAULT))
    top = {name for name, _ in rows[:k]}
    keep = top | ALWAYS_MP_NORMALS

    updated = 0
    for path in sorted(root.glob("*.cs")):
        text = path.read_text(encoding="utf-8")
        if ": NormalMonsterCard" not in text or "abstract class" in text:
            continue
        name = path.name
        want = name in keep
        has = "YgoCardPackTags.MultiplayerSafe" in text
        if want and not has:
            new_text = ensure_mp(text)
        elif not want and has:
            new_text = strip_mp(text)
        else:
            continue
        path.write_text(new_text, encoding="utf-8", newline="\n")
        updated += 1

    print(f"Patched {updated} normal monster files (keep set size {len(keep)} of {n} scored normals; fraction={tier.PERCENTILE_DEFAULT})")


if __name__ == "__main__":
    main()
