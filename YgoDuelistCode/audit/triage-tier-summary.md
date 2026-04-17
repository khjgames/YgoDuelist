# Triage by tier (plan: wave refactors)

Master file list: `all-cs-files.txt` (optional; may be regenerated manually via `audit/Run-CardLogicAudit.ps1` if you use it).

## Tier 1 — `Patches/**`

| Status | Notes |
|--------|--------|
| Machine triage | See `triage-patches-grep.txt` (underscore card types + `Keldo`). |
| Wave 0 done | `YgoFusionGateFieldGlowPatch` — dispatches only via `IYgoNHandPlayPhaseHighlightOverride`; no `Fusion_Gate` / `Special_Summon_Egyptian_God_Slime` branches in patch. |
| Wave 1a done | `DuelMonsterPetDeathPatch` (Keldo / Ox), `PlayCardFromSpellTrapZonePatch` (Burst/Diffusion cancel flag), `HookAfterCardPlayedEffectMonsterPatch` (deferred block interface), `CardPileCmdFieldMonsterGraveyardEffectsPatch` (pile move virtual). |
| Wave 1b done | Net checksum Die For You (`ReconcileDieForYouChecksumForPet`), option pile command hooks, Activate Effect text/title on command types, deserialize printed stats virtual, Convulsion preview interface, strike/defend energy path, alternate upgraded description on `MonsterCommandCard`. |
| Wave 2 done | IsValidTarget refinement (`RefineIsValidTarget` + `CardModelIsValidTargetYgoRefinePatch`); GY hooks (`IYgoAfterFaceDownSetTrapDestroyedToGraveyardAsync`, `IYgoAfterMonsterMovedToGraveyardFromHandOrDraw`, `ScheduleDrawCardsEqualToPrintedMgcWhenMovedFromHandToGraveyard`); see `triage-patches-grep.txt`. |
| Wave 5 done | `CardPileAddInternalSkullInvitationPatch` now dispatches `YgoGraveyardPileHooks.DispatchCardAddedHook`; concrete Black Pendant service branch removed from patch list. |
| Follow-up | Periodic re-grep Patches/** for new concrete branches (e.g. `PlayCardAction*` patches). |

## Tier 2 — `Services/**`

| Status | Notes |
|--------|--------|
| Machine triage | See `triage-services-grep.txt`. |
| Wave 1 sample | `MonsterCommandRegistry` turn-end field clears, `YgoEquipSpellTargetRules`, `YgoGoraTurtleService` (see triage file). |
| Wave 2 done | Queue deferral, Neow signature markers, banish hook, tribute/Mausoleum interfaces, Guardian Slime GY hook on `BaseMonsterCard`, flip pipeline, Sealmaster/talisman markers, Slifer/Legion markers — see `triage-services-grep.txt`. |
| Wave 5 done | Added `IYgoOnAddedToYgoGraveyardPile` and moved `Black_Pendant` graveyard rule to card-owned hook; removed obsolete `YgoBlackPendantGraveyard` concrete service. |
| Wave 6 audit | 10-card PackTags spell/trap batch reviewed; no additional non-orchestrator concrete service branches found. |
| Wave 7 done | `YgoCurseOfDarknessField`/`YgoCurseOfDarknessSpellHook` now use `IYgoSpellResolvedOwnerDamageContributor` instead of concrete `Curse_of_Darkness` type checks; 10-card trap batch recorded. |
| Wave 8 done | 25-card PackTags trap batch reviewed; `YgoNarrowPassField` generalized via `IYgoMonsterCommandFieldTaxContributor` (removed concrete `Narrow_Pass` service checks). |
| Wave 9 audit | 25-card PackTags spell batch reviewed; no additional non-orchestrator concrete service branches found. |
| Wave 10 done | 35-card PackTags spell/ritual batch reviewed; `YgoJamBreedingMachineContinuous` generalized via `IYgoOwnerTurnStartSpellTrapZoneEffect` (removed concrete `Jam_Breeding_Machine` service checks). |
| Wave 11 done | 35-card PackTags spell batch reviewed; `YgoDealWithDarkRulerState` generalized via `IYgoOwnerTurnStartFieldMonsterEffect` (removed concrete `Berserk_Dragon` service checks). |
| Wave 12 done | 35-card mixed PackTags batch reviewed; `YgoCardTraderContinuous` removed and `Card_Trader` moved onto `IYgoOwnerTurnStartSpellTrapZoneEffect` via `YgoJamBreedingMachineContinuous` dispatch. |
| Wave 13 done | 35-card mixed PackTags batch reviewed; `GraveyardRelic.BeforeFlush` end-phase chain replaced with `YgoOwnerBeforeTurnEndFlushHooks` and card-owned field/graveyard/banished interfaces; obsolete concrete end-phase services removed. |
| Wave 14 done | 35-card trap PackTags batch reviewed; `YgoBlindDestructionContinuous` removed and `Blind_Destruction` moved onto `IYgoOwnerTurnStartSpellTrapZoneEffect` via existing `YgoJamBreedingMachineContinuous` dispatch. |
| Wave 15 done | 35-card mixed PackTags batch reviewed; `GraveyardRelic.AfterPlayerTurnStart` same-timing bucket normalized via `YgoOwnerTurnStartFieldMonsterHooks` and card-owned `IYgoOwnerTurnStartFieldMonsterEffect` (`Slifer_the_Sky_Dragon`, `Gora_Turtle`, `Berserk_Dragon`); obsolete `YgoGoraTurtleService` removed. |
| Wave 16 done | 35-card mixed PackTags batch reviewed; `GraveyardRelic.BeforeFlush` same-timing bucket normalized by extending `YgoOwnerBeforeTurnEndFlushHooks` to spell/trap-zone hooks via `IYgoOwnerBeforeTurnEndFlushSpellTrapZoneEffect`; `Bottomless_Shifting_Sand` moved to card-owned hook and concrete `YgoBottomlessShiftingSandContinuous` removed. |
| Wave 17 done | 35-card effect-monster PackTags batch reviewed; `GraveyardRelic.AfterPlayerTurnStart` spell/trap zone dispatch split into phased `YgoJamBreedingMachineContinuous.TryResolvePlayerTurnStartForPhase` + `YgoOwnerTurnStartSpellTrapDispatchPhase`; `The_Sanctuary_in_the_Sky` owns early-phase `IYgoOwnerTurnStartSpellTrapZoneEffect` (mercury draw) instead of inlined relic checks. |
| Follow-up | Periodic re-grep Services for new concrete card branches. |

## Tier 3 — `Relics/**`, `Character/**`, `Powers/**`

| Status | Notes |
|--------|--------|
| Grep pass | See `triage-relics-character-powers-grep.txt` (2026-04-16): Relics/Character clean; Powers hit `FairyBoxFieldPower` only. |
| Wave 2 pass | No new edits — still aligned with prior triage; re-grep when touching these folders. |

## Tier 4 — `Cards/**`

| Status | Notes |
|--------|--------|
| Clean-by-default | Card logic lives on card classes; use `grep` only for unusual cross-card coupling. |
| Wave 2 | New overrides/interfaces added on card types (Bottomless, Dark Mirror, Statue, Fear, Electric Snake, Elephant statues); no broad Cards/** sweep this batch. |
| Wave 5 queueing | Added `audit/packtags-review-ledger.md` to track PackTags reviewed/unreviewed card batches across Monster/Spell/Trap. |
| Wave 6 queueing | Logged a 10-card Spell/Trap PackTags audit batch in `packtags-review-ledger.md` (minimum batch size now 10). |
| Wave 7 queueing | Logged an additional 10-card trap PackTags batch in `packtags-review-ledger.md` including one service-scope refactor (`Curse_of_Darkness`). |
| Wave 8 queueing | Logged a 25-card trap PackTags batch in `packtags-review-ledger.md` and raised minimum batch target to 25. |
| Wave 9 queueing | Logged a 25-card spell PackTags batch in `packtags-review-ledger.md`. |
| Wave 10 queueing | Logged a 35-card spell/ritual PackTags batch in `packtags-review-ledger.md` and raised minimum batch target to 35. |
| Wave 11 queueing | Logged a 35-card spell PackTags batch in `packtags-review-ledger.md`. |
| Wave 12 queueing | Logged a 35-card mixed PackTags batch in `packtags-review-ledger.md` including Card Trader turn-start refactor. |
| Wave 13 queueing | Logged a 35-card mixed PackTags batch in `packtags-review-ledger.md` including owner before-turn-end flush interface refactor. |
| Wave 14 queueing | Logged a 35-card trap PackTags batch in `packtags-review-ledger.md` including Blind Destruction turn-start refactor. |
| Wave 15 queueing | Logged a 35-card mixed PackTags batch in `packtags-review-ledger.md` including owner turn-start field-monster dispatch normalization. |
| Wave 16 queueing | Logged a 35-card mixed PackTags batch in `packtags-review-ledger.md` including owner before-turn-end spell/trap-zone dispatch normalization. |
| Wave 17 queueing | Logged a 35-card effect-monster PackTags batch in `packtags-review-ledger.md` including phased owner turn-start spell/trap dispatch + Sanctuary refactor. |

---

**Definition of done (per tier):** triage notes recorded; known hotspots either refactored or listed under Follow-up.
