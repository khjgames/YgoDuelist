# GraveyardRelic: card-centric hooks (design)

Reference pattern: `BaseMonsterCard.OnSummoned` — engine calls one virtual on the card; default no-op; per-card logic stays on the card class.

Target: `YgoDuelistCode/Relics/RelicsForStarter/GraveyardRelic.cs` should orchestrate phase order and shared state only (`_splinterChainRunning`, `_onDamageEffectSeenEnemyIds`, `TryConsumeAnnual`), not `if (monster is ConcreteCard)` chains.

## Proposed hooks (on `BaseMonsterCard` / `AbstractMonsterCard` unless noted)

| GraveyardRelic area | Replace | Hook / pattern |
|---------------------|---------|----------------|
| `AfterPlayerTurnStart` — field Mercury scan, Cure Mermaid per-pet, GY Darklord Marie | per-card `if` | `OnOwnerTurnStartOnFieldAsync(Player, PlayerChoiceContext, Creature pet)`; GY: `IGraveyardTurnStartEffect` or loop + interface |
| `AfterAttack` before splinter — Spirit of the Breeze, D.D. Warrior Lady | per-card `if` | `Task OnGraveyardRelicAfterAttackOpeningAsync(AttackCommand, Player, BlockingPlayerChoiceContext)` |
| `ProcessMonsterUnblockedOnDamageEffectsAsync` — Bistro Butcher, Masked Sorcerer; Cestus on equip | per-card `if` | Monster: `OnFirstUnblockedDamageToEnemyThisChainAsync(...)`; Equip: `IEquipFirstUnblockedDamageEffect` on `BaseEquipSpellCard` |
| `ProcessMonsterExecuteKillEffectsAsync` — Insect Princess, Timeater, Shinato/Des Volstgalph, Ra, Twin-Headed Wolf, Guardian Angel Joan | per-card `if` after generic execute-ATK props | `Task OnEnemyExecutedByThisAttackAsync(AttackCommand, CombatState)` — re-entrant safe with splinter nested commands |

Splinter/blight from `AttackDealsSplinterDamage` and equip `GrantsSplinterTo` / `GrantsBlightTo` stay data-driven; no extra virtuals unless a card needs custom rules.

## Migration order (implementation)

1. Add `OnEnemyExecutedByThisAttackAsync`; migrate one card (e.g. Guardian Angel Joan or Timeater), then batch the rest. Remove matching `if` branches from `GraveyardRelic` in the same PR as each card.
2. Add opening + first-unblocked hooks; migrate Spirit of the Breeze, D.D. Warrior Lady, Bistro Butcher, Masked Sorcerer; Cestus via equip interface.
3. Turn start: Cure Mermaid, Darklord Marie, Mercury/Sanctuary (field scan may stay a small relic branch until field API is cleaner).

## Rules

- When a card is migrated, delete the old `if (monster is Foo)` from `GraveyardRelic` in the same change set (no duplicate paths).
- Equips: no `if (eq is Cestus_of_Dagla)` in relic — use equip interface or virtual on `BaseEquipSpellCard`.

## Related (same principles)

- `MonsterCommandRegistry.ResolveKarateManEndOfTurnDestructionAsync` / `ResolveGuardianSlimeEndOfTurnDestructionAsync` → card `OnOwnerTurnEndMayDestroySelfAsync` or `IEndOfTurnFieldDestruction`.
- Continuous spell/trap services: move logic toward spell/trap card model over time; relic delegates.
