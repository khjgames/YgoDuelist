# Card integration playbook (YgoDuelist)

Where card-specific behavior should live, and how Harmony patches should delegate.

## Virtual methods on a card base

Use when **every** card of that lineage might override behavior, and the engine calls **one** method on the model.

- **Examples:** `BaseMonsterCard.OnSummoned` / `OnAfterSummonPipelineAsync` (summon effects); `AbstractMonsterCard` pet/stance/flip hooks; `YgoDuelistCard.GetNHandPlayPhaseHighlightModulateOverride` (play-phase hand outline).
- **Caller:** Services or patches call the virtual once; **no** `if (model is Some_Card)` in the patch.

## Small interfaces (`IYgo…`)

Use when behavior is **optional** or a **capability** shared across unrelated bases (e.g. `MonsterCommandCard` is not a `YgoDuelistCard`).

- **Examples:** `IYgoPrePlayCancelableGridSelection`, `IYgoCardZoneRightClick`, `IYgoAfterDuelMonsterDiedZoneCard`, `IYgoNHandPlayPhaseHighlightOverride`.
- **Patch pattern:** `if (model is IYgoSomething x) { x.Method(...); }` — **one** interface check, not one check per card type.

## Domain services

Use for **orchestration** (fusion selection, pile moves, grids) that many cards share. Card classes should **call** services or implement **thin** hooks that call services — avoid duplicating full rules in patches.

## Anti-patterns (red)

- Named card types in patches: `if (model is Fusion_Gate)` with multi-step rules.
- Same for Services when the block is really **one card’s rule** — move to that card (virtual or interface).

## Green references in this repo

| Pattern | Location |
|---------|----------|
| Summon | `BaseMonsterCard.OnSummoned`, `DuelMonsterSummon` |
| Pre-play grid | `IYgoPrePlayCancelableGridSelection`, `PlayCardActionPrePlayCancelableGridPatch` |
| Zone right-click | `IYgoCardZoneRightClick`, `SpellTrapCardRightClickPatch` |
| Zone on duel monster death | `IYgoAfterDuelMonsterDiedZoneCard`, `DuelMonsterPetDeathPatch.NotifyZoneCardsAfterDuelMonsterDied` |
| Play-phase hand glow | `IYgoNHandPlayPhaseHighlightOverride`, `YgoFusionGateFieldGlowPatch`, `Fusion_Gate`, `Special_Summon_Egyptian_God_Slime` |

## Audit artifacts

- `YgoDuelistCode/audit/all-cs-files.txt` — full checklist.
- `YgoDuelistCode/audit/triage-patches-grep.txt`, `triage-services-grep.txt` — machine triage hints.
- `YgoDuelistCode/audit/triage-tier-summary.md` — tier status.
