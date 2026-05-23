# YgoDuelist Replay Mechanic Interactions

Reference for how **Replay** (Glam, Spiral, base replay, powers, relics, etc.) interacts with **IYgoCard** cards.

## Problem

Vanilla STS2 Replay runs `OnPlayWrapper` in a loop on the **same** `CardModel`. That works for cards that resolve and leave play. YGO cards become **persistent zone objects** (monster field, spell/trap zone, equip links). Two plays on one instance bind two zone slots to one model and break summons, continuous traps, and equip logic.

## Core rule

For every **IYgoCard**, every replay source:

1. **Suppress** the vanilla same-instance replay loop (force one paid `OnPlay` resolution).
2. After the first play **fully resolves**, for each replay count **sequentially**:
   - Spawn a **real, persistent** combat `CardModel` clone (not ephemeral `IsDupe`).
   - Try to play it **for free** into the correct zone.
   - If zone space or required targets are missing → **fizzle to graveyard**, no effect.
   - If legal → resolve with the category rules below.

**One-line summary:** One paid play → full resolve once → each replay spawns a free real duplicate into an open zone (STZ for spells/traps with full re-selection; monster zone for summons with no re-cost) or fizzles to GY.

## Timing

Replay always happens **after** the first resolution completes, not mid-chain. Stacked replay (+2) runs duplicate attempts one after another; each checks zone space independently.

## Category rules

### Spells and traps (hand or STZ activation)

Includes normal spells (e.g. Raigeki, Monster Reborn), continuous cards (Call of the Haunted), equip spells, and traps activated from hand or zone.

| | First play | Each replay |
|---|---|---|
| Cost | Pay energy / stars | **Free** |
| Zone gate | Normal hand/STZ rules | Requires **empty non-field STZ slot** (field spells use field slot) |
| Resolution | Full play logic | Full play logic again |
| Selections | Normal | **New selection** (first target often moved by first resolve) |
| Persistence | Normal GY/STZ fate | Real card; GY if fizzles or after resolve |

Normal spells that never occupy STZ still require an **available STZ slot** for the replay duplicate to “play from,” matching the rule that hand spells/traps need zone space to activate.

### Equip spells (replay-specific)

- Duplicate enters open STZ, then resolves.
- Target selection is offered; **cancel → default to first play’s target**.
- If first target is gone and **no valid targets remain** → fizzle to GY.

### Monsters (normal / tribute / special / set-summon)

| | First play | Each replay |
|---|---|---|
| Costs / selections | Pay once (energy, tributes, special gates, grids) | **None** |
| Zone gate | Normal summon rules | **Empty monster zone** |
| Body | First summon | **Exact clone** (stats, form, battle position, attack/block target) |
| Hand-summon combat | Attack → damage target; defend → block | **Same combat action again** on each replay duplicate (via **`NormalMonsterCard.CombatAction`**, free; no Narrow Pass re-pay) |
| Stiff / fatigue | Normal hand/tribute summon → stiff after summon | **Same `canAttackThisTurn` flag** captured from first **`TrySummonDuelMonster`**; replay duplicate gets stiff/fatigue when the original did |
| Triggers | Full summon/flip/trigger logic | **Yes — fire again** |
| Persistence | Normal field monster | Real monster (tributable, destroyable, commands) |

Fantasy: *as if a second identical copy was played from the same place, immediately, for free.*

**Hand-summon combat action:** When the first play is a **`NormalMonsterCard`** hand summon that resolves **`CombatAction`** (attack position → deal damage to the selected enemy; defense position → grant block), each replay duplicate **summons then runs the same `CombatAction` for free** against the **same attack target** (if still alive). Fusion/ritual/option-output summons and summons that skip post-summon combat (e.g. tribute fallback paths that never call **`CombatAction`**) do not replay combat. Capture is via **`YgoReplayPatch`** on **`NormalMonsterCard.CombatAction`** during the first paid resolve.

**Stiff / fatigue:** When the first hand summon used **`canAttackThisTurn: false`** (normal/tribute default), the duplicate receives the same stiff/fatigue via **`TrySummonReplayDuplicateAsync`** passing the captured flag into **`TrySummonDuelMonster`**. Cards with **`SpecialSummonGrantsImmediateCommandsThisTurn`** or **`NormalSummonSkipsStiffFatigueOnSummonTurn`** follow the same rules as the first play.

### Fusion / ritual spells

First play pays materials and energy once. Each replay duplicates the **summoned fusion/ritual monster** (monster pipeline), not a second copy of the spell in STZ. Material selection is **not** repeated.

### Option-pile special summons (e.g. Special Summon Dark Sage)

First play pays the command cost once. Each replay duplicates the **summoned output monster**, not another command card in the option pile.

## Set vs activate

| Action | Replay? |
|---|---|
| **Set monster** (defense / face-down summon) | **Yes** — monster replay rules |
| **Set spell/trap** face-down (hold) | **No** — not a play |
| **Activate** spell/trap from hand or STZ | **Yes** — spell/trap replay rules |

## Fizzle

When the duplicate cannot enter the required zone or has no valid targets when targets are required: **no effect**, card goes to **graveyard**.

## Explicit non-goals

- Vanilla STS replay loop on one `CardModel`
- Ephemeral `IsDupe` that vanishes after resolve
- Re-charging tribute / material / special-summon costs on monster replay
- Replay on set spell/trap (face-down hold only)

## Multiplayer

Replay drain runs in an **awaited** chain on all peers (same action order). Zone checks use shared helpers (`HasSpaceForSetOrPlay`, `HasRoomForDuelSummonAfterReleasing`). Selection grids use synced helpers (`YgoPrePlayGridSelection`, `EquipSpellGridSelect`, etc.).

## Implementation

- [`YgoReplayCoordinator`](../YgoDuelistCode/Services/YgoReplayCoordinator.cs) — capture replay count, route, sequential drain
- [`YgoSpellTrapReplayFlow`](../YgoDuelistCode/Services/YgoSpellTrapReplayFlow.cs) — STZ duplicate resolve
- [`YgoMonsterReplayFlow`](../YgoDuelistCode/Services/YgoMonsterReplayFlow.cs) — monster duplicate summon
- [`YgoReplayPatch`](../YgoDuelistCode/Patches/PatchesForCards/YgoReplayPatch.cs) — Harmony intercept

## Related (unchanged)

[`Double_Spell`](../YgoDuelistCode/Cards/Spell/Done/Normal/Double_Spell.cs) — card-specific GY spell replay; not enchantment replay.

## See also

- [`Card_integration_playbook.md`](Card_integration_playbook.md) — patch/service conventions
- [`Multiplayer_playbook.md`](Multiplayer_playbook.md) — MP test matrix
- [`Replay_Implement_Phase2.md`](Replay_Implement_Phase2.md) — verification checklist and open items
