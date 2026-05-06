# Level/Race/Attribute Modifier Model

## Rule

Calculation modifiers must be modeled as additive totals, not destructive base rewrites.

- Level modifiers are applied as LevelEffectTotals.
- Race modifiers are applied as RaceEffectTotals.
- Attribute modifiers are applied as AttributeEffectTotals.

Never permanently mutate printed/base values to simulate temporary effects.

## Cost Down Specification

Cost Down applies a power on the player:

- `CostDownHandLevelPower` stack count = number of active Cost Down effects this turn.
- Hand monster effective level reduction = `2 * Amount`.
- Hand monster energy discount = `1 * Amount`.
- Effective level is clamped to minimum 1 for gameplay display/rules.

When a monster is summoned from hand while Cost Down is active:

- It is summoned with its normal printed summon data.
- On summon, it receives `CostDownSummonedLevelPower` equal to the current hand-level reduction.
- HP/max HP are recalculated from the current effective level.

At end of owner turn:

- Player `CostDownHandLevelPower` is removed.
- `CostDownSummonedLevelPower` is removed from that player's duel monsters.
- Level and max HP/current HP are recalculated from restored effective level.
- HP increase from max HP growth is recovered by normal max-HP delta sync logic.

## A Legendary Ocean Specification

While `A Legendary Ocean` is face-up:

- Affected WATER duel monsters receive `LegendaryOceanLevelPower`.
- `LegendaryOceanLevelPower` amount equals the total level reduction from active copies.
- HP/max HP are recalculated immediately when the field state changes and on summon.

When no longer active:

- `LegendaryOceanLevelPower` is removed.
- Level and HP/max HP recalculate to restored values.

## Implementation Notes

- Modifier stacks are treated as totals. Do not apply raw minus operations that require compensating plus operations later.
- End-of-turn restoration and field-toggle restoration must be driven by recomputation from active totals.
- Use the same pattern for future race/attribute-changing effects.

## DNA Surgery / DNA Transplant Specification

- `DNA Surgery` declares one race via a selection UI and applies a continuous field override while face-up.
- `DNA Transplant` declares one attribute via a selection UI and applies a continuous field override while face-up.
- Overrides apply only to face-up duel monsters on the field and are removed immediately when the trap is inactive.
- Effective race/attribute are computed from override totals, not by mutating printed card data.
- Any system that depends on race/attribute (for example `A Legendary Ocean` WATER checks) must read effective values and recalculate when these totals change.
