"""Pick ~20 lowest-risk cards per pack theme (matching category quotas) and add MultiplayerSafe to PackTags in .cs sources.

This is a separate workflow from tools/generate_user_mp_safe_explicit.py, which emits the explicit
type set consumed by YgoMultiplayerSafePackRules + GetEffectivePackTags without editing card files.
"""
from __future__ import annotations

import re
from pathlib import Path
from collections import defaultdict

ROOT = Path(__file__).resolve().parents[1]
CARDS = ROOT / "YgoDuelistCode" / "Cards"

TAG_NAMES = [
    "Earth", "Water", "Wind", "Fire", "Dark", "Light", "Fusion", "Ritual",
    "Ocean", "Insect", "Machine", "Dragon", "Zombie", "Fiend", "Spellcaster",
    "Warrior", "Heal", "Draw", "Chance", "Burn", "Spell", "Trap",
]

STATS = {
    "Earth": {"fusion_m": 13 / 305, "ritual_m": 4 / 305, "normal_m": 199 / 305, "effect_m": 79 / 305, "spells": 6 / 305, "token_spells": 2 / 305, "traps": 2 / 305},
    "Water": {"fusion_m": 9 / 141, "ritual_m": 2 / 141, "normal_m": 93 / 141, "effect_m": 32 / 141, "spells": 5 / 141},
    "Wind": {"fusion_m": 11 / 91, "normal_m": 53 / 91, "effect_m": 25 / 91, "spells": 2 / 91},
    "Fire": {"fusion_m": 5 / 54, "ritual_m": 1 / 54, "normal_m": 28 / 54, "effect_m": 17 / 54, "spells": 3 / 54},
    "Dark": {"fusion_m": 23 / 283, "ritual_m": 10 / 283, "normal_m": 148 / 283, "effect_m": 91 / 283, "spells": 7 / 283, "token_spells": 1 / 283, "traps": 3 / 283},
    "Light": {"fusion_m": 11 / 116, "ritual_m": 3 / 116, "normal_m": 52 / 116, "effect_m": 43 / 116, "spells": 7 / 116},
    "Fusion": {"fusion_m": 69 / 86, "effect_m": 14 / 86, "spells": 3 / 86},
    "Ritual": {"ritual_m": 20 / 45, "effect_m": 3 / 45, "ritual_spells": 22 / 45},
    "Ocean": {"fusion_m": 8 / 110, "ritual_m": 2 / 110, "normal_m": 74 / 110, "effect_m": 20 / 110, "spells": 5 / 110, "token_spells": 1 / 110},
    "Insect": {"fusion_m": 1 / 46, "ritual_m": 1 / 46, "normal_m": 27 / 46, "effect_m": 14 / 46, "spells": 2 / 46, "token_spells": 1 / 46},
    "Machine": {"fusion_m": 6 / 70, "normal_m": 48 / 70, "effect_m": 14 / 70, "spells": 2 / 70},
    "Dragon": {"fusion_m": 12 / 67, "ritual_m": 1 / 67, "normal_m": 31 / 67, "effect_m": 18 / 67, "spells": 4 / 67, "traps": 1 / 67},
    "Zombie": {"fusion_m": 5 / 55, "ritual_m": 1 / 55, "normal_m": 27 / 55, "effect_m": 16 / 55, "spells": 5 / 55, "traps": 1 / 55},
    "Fiend": {"fusion_m": 4 / 111, "ritual_m": 5 / 111, "normal_m": 64 / 111, "effect_m": 34 / 111, "spells": 2 / 111, "token_spells": 1 / 111, "traps": 1 / 111},
    "Spellcaster": {"fusion_m": 6 / 91, "ritual_m": 3 / 91, "normal_m": 45 / 91, "effect_m": 34 / 91, "spells": 3 / 91},
    "Warrior": {"fusion_m": 11 / 105, "ritual_m": 5 / 105, "normal_m": 51 / 105, "effect_m": 33 / 105, "spells": 4 / 105, "traps": 1 / 105},
    "Heal": {"effect_m": 13 / 22, "spells": 5 / 22, "token_spells": 1 / 22, "traps": 3 / 22},
    "Draw": {"effect_m": 44 / 68, "spells": 17 / 68, "traps": 7 / 68},
    "Chance": {"fusion_m": 1 / 13, "effect_m": 8 / 13, "spells": 1 / 13, "traps": 3 / 13},
    "Burn": {"fusion_m": 2 / 112, "effect_m": 75 / 112, "spells": 20 / 112, "traps": 15 / 112},
    "Spell": {"ritual_m": 1 / 153, "normal_m": 1 / 153, "effect_m": 31 / 153, "spells": 109 / 153, "token_spells": 6 / 153, "traps": 5 / 153},
    "Trap": {"normal_m": 1 / 63, "effect_m": 9 / 63, "spells": 4 / 63, "traps": 49 / 63},
}

RISK_RES = [
    re.compile(r"BlightPower", re.I),
    re.compile(r"AttackDealsBlightedDamage|AttackDealsSplinterDamage|AttackDealsFullBlighted", re.I),
    re.compile(r"GrantsBlight|GrantsSplinter", re.I),
    re.compile(r"Apply<BlightPower>", re.I),
    re.compile(r"ApplyHalfBlightToAllEnemiesOnExecuteKill", re.I),
    re.compile(r"UnityEngine\.Random|System\.Random", re.I),
]

EXCLUDE_STEMS = {
    "Copycat",
    "FlatRaceEquipSpells",
    "FlatRaceFieldSpells",
    "Needle_Burrower",
}


def classify_path(p: Path) -> str | None:
    s = str(p).replace("\\", "/")
    if "/TrapMonster/" in s:
        return "trap_m"
    if "/Monster/Done/Fusion/" in s:
        return "fusion_m"
    if "/Monster/Done/Ritual/" in s:
        return "ritual_m"
    if "/Monster/Done/Normal/" in s:
        return "normal_m"
    if "/Monster/Done/Effect/" in s:
        return "effect_m"
    if "/Spell/" in s:
        if "/Ritual" in s or "RitualSpell" in s:
            return "ritual_spells"
        if "Token" in p.stem or "/Token" in s:
            return "token_spells"
        return "spells"
    if "/Trap/" in s:
        return "traps"
    return None


def extract_pack_tags_block(text: str) -> str | None:
    m = re.search(r"(?<![A-Za-z])PackTags\s*=>\s*(.*?);", text, re.S)
    return m.group(1) if m else None


def risk_score(text: str, stem: str) -> int:
    if stem in EXCLUDE_STEMS:
        return 999
    score = 0
    for r in RISK_RES:
        if r.search(text):
            score += 5
    n = len(text)
    if n > 900:
        score += 1
    if n > 1600:
        score += 2
    return score


def quota_for_tag(tag: str, n: int = 20) -> dict[str, int]:
    fr = STATS[tag]
    keys = list(fr.keys())
    raw = {k: int(round(n * fr[k])) for k in keys}
    for k in keys:
        if fr[k] > 0 and raw[k] == 0:
            raw[k] = 1
    total = sum(raw.values())
    while total > n:
        drop_order = sorted(keys, key=lambda k: (-raw[k], -fr[k]))
        shrunk = False
        for k in drop_order:
            if raw[k] <= 0:
                continue
            if fr[k] <= 0.01 and raw[k] > 0:
                raw[k] -= 1
                total -= 1
                shrunk = True
                break
        if not shrunk:
            k = max(keys, key=lambda x: raw[x])
            if raw[k] > 0:
                raw[k] -= 1
                total -= 1
    while total < n:
        best = max(keys, key=lambda k: fr[k])
        raw[best] += 1
        total += 1
    return raw


def tag_in_block(tag: str, block: str) -> bool:
    return bool(re.search(rf"YgoCardPackTags\.{tag}\b", block))


def add_multiplayer_safe(content: str) -> tuple[str, bool]:
    if "MultiplayerSafe" in content:
        return content, False
    # Multiline: PackTags => \n tags
    m2 = re.search(
        r"(public override YgoCardPackTags PackTags =>)\s*\n(\s*)(?=YgoCardPackTags)",
        content,
    )
    if m2:
        i = m2.end(2)
        return content[:i] + "YgoCardPackTags.MultiplayerSafe | " + content[i:], True
    m1 = re.search(
        r"(public override YgoCardPackTags PackTags =>)\s+(?=YgoCardPackTags)",
        content,
    )
    if m1:
        i = m1.end(1)
        return content[:i] + " YgoCardPackTags.MultiplayerSafe |" + content[i:], True
    return content, False


def main() -> None:
    rows: list[dict] = []
    for path in sorted(CARDS.rglob("*.cs")):
        try:
            text = path.read_text(encoding="utf-8")
        except OSError:
            continue
        block = extract_pack_tags_block(text)
        if not block:
            continue
        if "MultiplayerSafe" in block:
            continue
        cat = classify_path(path)
        if cat is None:
            continue
        stem = path.stem
        rs = risk_score(text, stem)
        for tag in TAG_NAMES:
            if tag_in_block(tag, block):
                rows.append(
                    {
                        "path": path,
                        "tag": tag,
                        "cat": cat,
                        "risk": rs,
                        "stem": stem,
                    }
                )

    by_tag: dict[str, list[dict]] = defaultdict(list)
    for r in rows:
        by_tag[r["tag"]].append(r)

    to_patch: set[Path] = set()
    pick_log: list[str] = []

    for tag in TAG_NAMES:
        need = quota_for_tag(tag, 20)
        pool = [r for r in by_tag[tag]]
        pool.sort(key=lambda r: (r["risk"], r["stem"]))
        picked: list[dict] = []
        used: set[Path] = set()

        def take(cat: str, count: int) -> None:
            nonlocal picked
            for r in pool:
                if count <= 0:
                    break
                if r["path"] in used:
                    continue
                if r["cat"] != cat:
                    continue
                picked.append(r)
                used.add(r["path"])
                count -= 1

        for cat, cnt in sorted(need.items(), key=lambda x: -x[1]):
            take(cat, cnt)
        if len(picked) < 20:
            for r in pool:
                if len(picked) >= 20:
                    break
                if r["path"] in used:
                    continue
                picked.append(r)
                used.add(r["path"])

        pick_log.append(f"\n=== {tag} quota={need} picked={len(picked)} ===")
        for r in picked:
            pick_log.append(f"  risk={r['risk']:3d} {r['cat']:12s} {r['stem']}")
            to_patch.add(r["path"])

    patched = 0
    for path in sorted(to_patch, key=lambda p: str(p)):
        text = path.read_text(encoding="utf-8")
        new_text, ok = add_multiplayer_safe(text)
        if ok:
            path.write_text(new_text, encoding="utf-8")
            patched += 1

    summary = (
        "\n".join(pick_log)
        + f"\n\nUnique files patched: {patched} (union pick count {len(to_patch)})\n"
    )
    (ROOT / "tools" / "multiplayer_safe_apply_log.txt").write_text(summary, encoding="utf-8")
    print(summary)


if __name__ == "__main__":
    main()
