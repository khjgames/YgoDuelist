# Replay implementation — phase 2 verification

Checklist and open items after the universal YGO replay system lands.

## Manual verification matrix

| # | Scenario | Expected |
|---|---|---|
| 1 | Glam + normal summon | 2 field copies; tribute/energy paid once |
| 1b | Glam + hand summon attack (e.g. Molten Zombie) | Both bodies hit the same first-play target |
| 1c | Glam + hand summon defense | Both bodies grant block via **`CombatAction`** |
| 1d | Glam + hand summon stiff/fatigue | Replay duplicate gets stiff/fatigue when original normal hand summon did |
| 2 | Glam + Spiral (+2 replay) | Up to 3 bodies if zones allow; partial fizzle if not |
| 3 | Call of the Haunted + replay | 2 STZ copies; independent GY picks |
| 4 | Equip + replay | Second STZ copy; cancel target → first target; fizzle if none legal |
| 5 | Monster Reborn / Raigeki + replay | Replay needs STZ slot; resolves; normal → GY |
| 6 | Set monster (defense) + replay | Second set copy on field |
| 7 | Set spell/trap face-down | **No** replay |
| 8 | Fusion / ritual spell + replay | Second fusion/ritual body; no second material grid |
| 9 | Option-pile Dark Sage + replay | Second Dark Sage if monster zone available |

Run single-player first, then MP co-op with checksum watch.

## Open items

### Token monsters (`IYgoTokenMonster`)

Allowed when summon room exists. Confirm replay clones do not pollute run deck / trunk tracking incorrectly.

### Partial first-play failure

If `OnPlayWrapper` aborts mid-resolution (cancel, death, combat end), **no** replay drain should run.

### Continuous trap link state

Call of the Haunted replay: second copy picks from current GY; if pool empty → fizzle to GY.

### Field spell replacement

Replay field spell uses field slot (`HasSpaceForSetOrPlay` always true); existing field spell goes to GY per normal rules.

### MP checksum

Add replay-spawned cards to regression notes if checksum drift appears; fingerprint via existing combat card registration.

### Opt-out interface

Add `IYgoOptOutOfReplay` only if a specific card proves incompatible after testing — not preemptively.

## Code touchpoints for debugging

- `YgoReplayCoordinator` — replay count capture, route, drain
- `YgoReplayPatch` — `Hook.ModifyCardPlayCount`, `OnPlayWrapper`, `RegisterSummon`, **`NormalMonsterCard.CombatAction`** capture
- `YgoSpellTrapReplayFlow` — STZ gate + pre-play grids + equip target
- `YgoMonsterReplayFlow` — monster zone gate + `TrySummonReplayDuplicateAsync` + replay **`CombatAction`**
