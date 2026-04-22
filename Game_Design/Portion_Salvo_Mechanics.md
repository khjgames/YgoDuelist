# Portion (salvo) mechanics

Portion is the in-game keyword for **one attack’s damage split into N sequential hits** (N = 2–5) that **sum to the same total** as a single hit would (integer remainder spread to the last hits; see `YgoPortionMath.SplitTotalIntoPortions`).

This document uses **Portion** for the player-facing name. Internal code and relic comments may still say **salvo** for the same state machine (`YgoPortionedSalvo`).

## Logical attack

A Portioned monster attack or a Portioned card (`CardDamagePortionCount` 2–5 on `YgoDuelistCard`) is still **one logical attack** for:

- Stiff / fatigue and similar “per attack” costs on the monster or player.
- **Narrow Pass field** and **Tyrant Dragon**-style effects: those increase **outer resolution count** (`GetAttackDefendResolutionCount` / whole `CombatAction` loops). Portion does **not** multiply into “N separate full attacks” inside one resolution.
- `NormalMonsterCard.OnAfterMonsterAttackHitAsync`: runs **once** per target after `YgoPortionDamage.DealMonsterAttackToTargetAsync` returns (all chunks have resolved).

Each `DamageCmd.Attack` chunk still produces its own `AttackCommand` and runs `GraveyardRelic.AfterAttack` per chunk so block, VFX, and per-chunk damage results stay faithful.

## Per-hit vs chain-wide hooks (`GraveyardRelic`)

- **Opening block** (`OnGraveyardRelicAfterAttackOpeningAsync`, `_onDamageEffectSeenEnemyIds.Clear`, first-chunk-only equip hooks): runs on the **first** chunk only. Mid-chunks use `YgoPortionedSalvo.IsMidMonsterSalvoPastFirstChunk` so the opening pass is skipped.
- **Per-enemy “first unblocked this chain”** (`ProcessMonsterUnblockedOnDamageEffectsAsync`): still keyed by `_onDamageEffectSeenEnemyIds` across the whole main hit + splinter chain; Portion chunks do not reset that set between chunks of the same salvo.

## Blight

When the attacker is Blighted (or Secret Pass–style Blight multiplier applies), Blight stacks are applied **once per enemy per logical attack** after the **last** Portion chunk, using **aggregated** incoming hit damage per enemy (`YgoExecuteKillShared.FullIncomingDamage` summed across chunks). See `GraveyardRelic` branches guarded by `consumedPortionFinal` / `TryConsumeMonsterSalvoFinal`.

## Splinter

Splinter starts **only after** the final Portion chunk (`!morePortionRemain` in `AfterAttack_FromDuelMonsterAsync`). There is **one** Splinter chain per logical attack, not one per small hit.

### Rule A (implemented)

The first Splinter budget uses **aggregated past-block on the primary target**: sum over all chunks of `(UnblockedDamage + OverkillDamage)` to that target for that attack. This matches sequential combat results and is cheap to compute.

### Rule B (not implemented)

A stricter reading would use **past-block from a hypothetical single hit** of the full damage against the defender’s block snapshot at attack start. That can differ from Rule A when block is chipped away between chunks. Rule B would need an explicit damage-preview API; document here so it is not confused with Rule A.

## Spells and traps

Opt in with `CardDamagePortionCount` (2–5) and route damage through `YgoPortionDamage.DealCardAttackDamageAsync`. Cards that intentionally fire **multiple separate** `DamageCmd.Attack` calls for different rules should **not** set Portion unless each call is meant to be one salvo.

Examples in this mod: `Strike_YgoDuelist` (Portion 2), `Burst_Breath` (Portion 3 per enemy).

## Combat cleanup

`YgoPortionedSalvo.ClearAll()` runs on `GraveyardRelic.AfterCombatEnd` so abandoned salvos cannot leak into the next combat.

## Related files

- `YgoPortionMath.cs`, `YgoPortionedSalvo.cs`, `YgoPortionDamage.cs`
- Per-monster `AttackPortionCount` overrides on concrete `BaseMonsterCard` types (default 0)
- `GraveyardRelic.cs` (`AfterAttack` aggregation)
- `Splinter_Damage_Mechanics.md` (bounce decay after the first Splinter hop)
