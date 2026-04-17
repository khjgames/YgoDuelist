# Card integration playbook (YgoDuelist)

Where card-specific behavior should live, and how Harmony patches should delegate.

## Virtual methods on a card base

Use when **every** card of that lineage might override behavior, and the engine calls **one** method on the model.

- **Examples:** `BaseMonsterCard.OnSummoned` / `OnAfterSummonPipelineAsync` (summon effects); `AbstractMonsterCard` pet/stance/flip hooks; `YgoDuelistCard.GetNHandPlayPhaseHighlightModulateOverride` (play-phase hand outline); `YgoDuelistCard.RefineIsValidTarget` (narrow `CardModel.IsValidTarget` after vanilla).
- **Caller:** Services or patches call the virtual once; **no** `if (model is Some_Card)` in the patch.

## Small interfaces (`IYgo…`)

Use when behavior is **optional** or a **capability** shared across unrelated bases (e.g. `MonsterCommandCard` is not a `YgoDuelistCard`).

- **Examples:** `IYgoPrePlayCancelableGridSelection`, `IYgoPlayCardActionPreSpendResourceFlow`, `IYgoTurnStartAtkGrowthFromFieldMonsterAfterCommandReset`, `IYgoPetDebuffPowerAmountReceivedHook`, `IYgoCardZoneRightClick`, `IYgoAfterDuelMonsterDiedZoneCard`, `IYgoNHandPlayPhaseHighlightOverride`.
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

## Additional green references

| Pattern | Location |
|---------|----------|
| Activate Effect pile UI | `IActivateEffectPileUi`, `ActivateEffectCardTextPatch`, `Activate_Effect` / `Activate_Effect_2` |
| Activate option-pile pre-play | `IActivateEffectPrePlayOptionPileCommand`, `PlayCardFromOptionPilePatch` |
| D.D. Scout Plane banish / end phase | `IYgoDdScoutPlaneCard`, `IYgoOwnerBeforeTurnEndFlushBanishedEffect`, `YgoOwnerBeforeTurnEndFlushHooks` |
| Fairy Box upkeep options | `IYgoFairyBoxUpkeepTakeDamageCommand`, `IYgoFairyBoxUpkeepDestroyTrapCommand`, `FairyBoxFieldPower` |
| GY from hand/field hook | `BaseMonsterCard.OnMovedToGraveyardFromHandOrField`, `CardPileCmdMonsterGraveyardHandFieldHookPatch` |
| IsValidTarget refinement | `YgoDuelistCard.RefineIsValidTarget`, `CardModelIsValidTargetYgoRefinePatch` |
| Trap destroyed set → GY | `IYgoAfterFaceDownSetTrapDestroyedToGraveyardAsync`, `CardPileCmdFaceDownSetTrapToGraveyardHookPatch` |
| Monster GY from hand or draw | `IYgoAfterMonsterMovedToGraveyardFromHandOrDraw`, `CardPileCmdMonsterGraveyardFromHandOrDrawHookPatch` |
| Hand → GY draw by printed Mgc | `BaseMonsterCard.ScheduleDrawCardsEqualToPrintedMgcWhenMovedFromHandToGraveyard` (e.g. Electric Snake, Elephant Statue of Blessing) |
| Pre-spend spell grids (Rush / Emergency / Riryoku / etc.) | `IYgoPlayCardActionPreSpendResourceFlow`, `PlayCardActionIYgoPreSpendResourceFlowPatch` |
| Simochi heal redirect | `IYgoBadReactionToSimochiHealRedirect`, `YgoBadReactionToSimochi`, `CreatureCmdHealBadReactionToSimochiPatch` |
| Post-heal field burn (Fire Princess) | `IYgoAfterOwnerPlayerCreatureHealGain`, `CreatureCmdHealFirePrincessPatch` |
| Option pile FTUE → monster menu | `MonsterCommandCard.TryEnqueueUnplayableOptionPileMenu`, `NCardPlayCannotPlayOptionPilePatch` |
| Turn-start ATK stack (field) | `IYgoTurnStartAtkGrowthFromFieldMonsterAfterCommandReset`, `MonsterCommandTurnResetPatch` |
| Pet debuff amount hook (ModifyPowerAmountReceived) | `IYgoPetDebuffPowerAmountReceivedHook`, `HookModifyPowerAmountReceivedTorpedoFishUmiPatch` |
| Command card `Title` getter | `MonsterCommandCard.ShouldPatchTitleToCardsTitleUpgradedLoc`, `TryPatchLocalizedTitleForCardModelTitleGetter`, `YgoFairyBoxUpkeepTitleUpgradedPatch`, `CommandChangeBattlePositionTitlePatch` |
| Card added to YGO graveyard pile | `IYgoOnAddedToYgoGraveyardPile`, `YgoGraveyardPileHooks.DispatchCardAddedHook`, `CardPileAddInternalSkullInvitationPatch` |
| Spell-resolved owner damage contributor | `IYgoSpellResolvedOwnerDamageContributor`, `YgoCurseOfDarknessField`, `YgoCurseOfDarknessSpellHook` |
| Monster command field tax contributor | `IYgoMonsterCommandFieldTaxContributor`, `YgoNarrowPassField`, `Narrow_Pass` |
| Owner turn-start spell/trap zone effect | `IYgoOwnerTurnStartSpellTrapZoneEffect`, `YgoOwnerTurnStartSpellTrapDispatchPhase`, `YgoJamBreedingMachineContinuous.TryResolvePlayerTurnStartForPhase`, `Jam_Breeding_Machine`, `The_Sanctuary_in_the_Sky` |
| Owner turn-start field monster effect | `IYgoOwnerTurnStartFieldMonsterEffect`, `YgoOwnerTurnStartFieldMonsterHooks`, `Berserk_Dragon`, `Slifer_the_Sky_Dragon`, `Gora_Turtle` |
| Owner before-turn-end flush field/graveyard/banished effect | `IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect`, `IYgoOwnerBeforeTurnEndFlushGraveyardEffect`, `IYgoOwnerBeforeTurnEndFlushBanishedEffect`, `YgoOwnerBeforeTurnEndFlushHooks`, `GraveyardRelic.BeforeFlush` |
| Owner before-turn-end flush spell/trap zone effect | `IYgoOwnerBeforeTurnEndFlushSpellTrapZoneEffect`, `YgoOwnerBeforeTurnEndFlushHooks`, `Bottomless_Shifting_Sand` |

## Audit folder (optional, at your pace)

`YgoDuelistCode/audit/` may contain a one-time or occasional file list (`all-cs-files.txt`), triage notes (`triage-*.txt`, `triage-tier-summary.md`), and an optional script `Run-CardLogicAudit.ps1` if you ever want to regenerate grep snapshots by hand. **Nothing here is required on build** and you should not maintain an ever-growing automated checklist — refactor card-scoping issues as you go, using the patterns above. Work tracking can live in your Cursor plan todos if you use that workflow.
