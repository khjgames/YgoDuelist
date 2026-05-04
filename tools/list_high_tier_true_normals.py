# Mirror YgoNormalMonsterPackTier + NormalMonsterCard.GetPackWeightMultiplierAdjusted for true normals.
# Default: print top PERCENTILE fraction by adjusted weight (MultiplayerSafe normal tier selection).
import argparse
import math
import re
from pathlib import Path

LOW_ATK = {0:0.7,1:0.85,2:1.0,3:1.25,4:1.1,5:0.6,6:0.8,7:0.95,8:1.1,9:1.05,10:0.8,11:0.8,12:0.9,13:0.6,14:0.6,15:0.65,16:0.7,17:0.75,18:0.85,19:1.0}
LOW_DEF = {0:0.6,1:0.75,2:0.9,3:1.2,4:1.2,5:0.5,6:0.9,7:1.15,8:1.1,9:0.75,10:0.75,11:0.85,12:0.55,13:0.6,14:0.65,15:0.7,16:0.75,17:0.85,18:0.9,19:1.0,20:1.15,21:1.25}
MED_ATK = {0:0.4,1:0.45,2:0.5,3:0.55,4:0.6,5:0.75,6:0.85,7:1.0,8:1.3,9:0.6,10:0.7,11:0.8,12:0.9,13:1.05,14:1.25,15:1.05,16:1.05,17:1.15,18:0.6,19:0.65,20:0.7,21:0.75,22:0.85,23:0.95,24:1.05,25:1.15,26:1.25}
MED_DEF = {0:0.4,1:0.45,2:0.5,3:0.55,4:0.6,5:0.75,6:0.85,7:1.0,8:0.6,9:0.7,10:0.8,11:0.9,12:1.05,13:1.25,14:1.05,15:1.05,16:1.15,17:0.6,18:0.65,19:0.7,20:0.75,21:0.85,22:0.95,23:1.05,24:1.15,25:1.25,26:1.35,27:1.45,28:1.55,29:1.65,30:1.75}
HIGH_ATK = {0:0.3,1:0.35,2:0.4,3:0.45,4:0.5,5:0.55,6:0.7,7:0.8,8:1.05,9:1.2,10:1.35,11:0.5,12:0.6,13:0.7,14:0.8,15:0.9,16:1.0,17:1.15,18:1.35,19:1.1,20:1.1,21:1.2,22:0.65,23:0.7,24:0.75,25:0.85,26:0.95,27:1.05,28:1.15,29:1.25,30:1.35}
HIGH_DEF = {0:0.35,1:0.4,2:0.45,3:0.5,4:0.55,5:0.7,6:0.8,7:1.05,8:1.2,9:1.35,10:0.5,11:0.6,12:0.7,13:0.8,14:0.9,15:1.0,16:1.15,17:1.35,18:1.1,19:1.1,20:1.2,21:0.65,22:0.7,23:0.75,24:0.85,25:0.95,26:1.05,27:1.15,28:1.25,29:1.35,30:1.45}

PERCENTILE_DEFAULT = 0.30
LEGACY_THRESHOLD = 1.1


def sw(d, k):
    return d.get(k, 1.0)


def compute_combined_tier(lv, atk, df):
    lv = max(lv, 1)
    if lv <= 4:
        atk_eff, def_eff = sw(LOW_ATK, atk), sw(LOW_DEF, df)
    elif lv <= 6:
        atk_eff, def_eff = sw(MED_ATK, atk), sw(MED_DEF, df)
    else:
        atk_eff, def_eff = sw(HIGH_ATK, atk), sw(HIGH_DEF, df)
    combined = max(atk_eff, def_eff) * 0.75 + min(atk_eff, def_eff) * 0.25
    return max(0.4, min(2.0, combined))


def adjusted_pack_weight(base, lv):
    w = base
    if lv <= 4:
        w -= 0.06
    elif lv <= 6:
        w -= 0.09
    else:
        w -= 0.03
    return w


def collect_scored_normals(root: Path) -> list[tuple[str, float]]:
    rows: list[tuple[str, float]] = []
    for path in sorted(root.glob("*.cs")):
        text = path.read_text(encoding="utf-8")
        if ": NormalMonsterCard" not in text or "abstract class" in text:
            continue
        m_lv = re.search(r"duelMonsterLevel:\s*(\d+)", text)
        m_atk = re.search(r"baseAtk:\s*(\d+)", text)
        m_def = re.search(r"baseDef:\s*(\d+)", text)
        if not (m_lv and m_atk and m_def):
            continue
        lv, atk, df = int(m_lv.group(1)), int(m_atk.group(1)), int(m_def.group(1))
        base = compute_combined_tier(lv, atk, df)
        adj = adjusted_pack_weight(base, lv)
        rows.append((path.name, adj))
    return rows


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "--mode",
        choices=("percentile", "legacy"),
        default="percentile",
        help="percentile: top --fraction by adjusted weight; legacy: adjusted >= 1.1",
    )
    ap.add_argument(
        "--fraction",
        type=float,
        default=PERCENTILE_DEFAULT,
        help="Top fraction for percentile mode (default 0.30)",
    )
    args = ap.parse_args()

    root = Path(__file__).resolve().parents[1] / "YgoDuelistCode" / "Cards" / "Monster" / "Done" / "Normal"
    rows = collect_scored_normals(root)
    rows.sort(key=lambda x: x[1], reverse=True)

    if args.mode == "legacy":
        for name, adj in rows:
            if adj >= LEGACY_THRESHOLD:
                print(name)
        return

    n = len(rows)
    k = max(1, math.ceil(n * args.fraction))
    for name, _ in rows[:k]:
        print(name)


if __name__ == "__main__":
    main()
