# Triage by tier (plan: wave refactors)

Master file list: `all-cs-files.txt` (optional; may be regenerated manually via `audit/Run-CardLogicAudit.ps1` if you use it).

## Tier 1 — `Patches/**`

| Status | Notes |
|--------|--------|
| Machine triage | See `triage-patches-grep.txt` (underscore card types + `Keldo`). |
| Wave 0 done | `YgoFusionGateFieldGlowPatch` — dispatches only via `IYgoNHandPlayPhaseHighlightOverride`; no `Fusion_Gate` / `Special_Summon_Egyptian_God_Slime` branches in patch. |
| Wave 1a done | `DuelMonsterPetDeathPatch` (Keldo / Ox), `PlayCardFromSpellTrapZonePatch` (Burst/Diffusion cancel flag), `HookAfterCardPlayedEffectMonsterPatch` (deferred block interface), `CardPileCmdFieldMonsterGraveyardEffectsPatch` (pile move virtual). |
| Wave 1b done | Net checksum Die For You (`ReconcileDieForYouChecksumForPet`), option pile command hooks, Activate Effect text/title on command types, deserialize printed stats virtual, Convulsion preview interface, strike/defend energy path, alternate upgraded description on `MonsterCommandCard`. |
| Follow-up | See `triage-patches-grep.txt` — periodic re-grep for new concrete branches. |

## Tier 2 — `Services/**`

| Status | Notes |
|--------|--------|
| Machine triage | See `triage-services-grep.txt`. |
| Wave 1 sample | `MonsterCommandRegistry` turn-end field clears, `YgoEquipSpellTargetRules`, `YgoGoraTurtleService` (see triage file). |
| Wave 2 done | Queue deferral, Neow signature markers, banish hook, tribute/Mausoleum interfaces, Guardian Slime GY hook on `BaseMonsterCard`, flip pipeline, Sealmaster/talisman markers, Slifer/Legion markers — see `triage-services-grep.txt`. |
| Follow-up | Periodic re-grep Services for new concrete card branches. |

## Tier 3 — `Relics/**`, `Character/**`, `Powers/**`

| Status | Notes |
|--------|--------|
| Grep pass | See `triage-relics-character-powers-grep.txt` (2026-04-16): Relics/Character clean; Powers hit `FairyBoxFieldPower` only. |

## Tier 4 — `Cards/**`

| Status | Notes |
|--------|--------|
| Clean-by-default | Card logic lives on card classes; use `grep` only for unusual cross-card coupling. |

---

**Definition of done (per tier):** triage notes recorded; known hotspots either refactored or listed under Follow-up.
