# Duel summon callsites — canonical vs one-off inventory

**Purpose:** Inventory every code path that performs a duel monster **special/normal summon** (including tokens), classify **shared infrastructure** vs **per-card one-offs**, and record which sites mirror `ReactorSlimeSummonGate` for **grid/playability parity** (vs relying only on `DuelMonsterSummon.TrySummonDuelMonster` enforcement).

**Rule:** Legal summon attempts always execute through `DuelMonsterSummon.TrySummonDuelMonster` / `TrySummonDuelMonsterSpecial` (the only mod path that calls `AddPetInternal` for duel pets). **`ReactorSlimeSummonGate.AllowsSummon`** runs inside that method. Card-local **`AllowsSummon` / `BlocksNonDivineSummons` in builders** avoids showing illegal targets and keeps MP choice chains aligned.

---

## A. Canonical / shared (not treated as one-offs)

| Location | Role |
|----------|------|
| `YgoDuelistCode/Services/ServicesForDuelMonsters/DuelMonsterSummon.cs` | Single enforcement point: `AllowsSummon`, `RecordSummon`, pet creation |
| `YgoDuelistCode/Services/YgoTokenSummon.cs` | Creates token card instance → `TrySummonDuelMonsterSpecial` |
| `YgoDuelistCode/Services/RitualSummonSelection.cs` | Ritual targets filtered in `GetRitualTargetsInHand` (`AllowsSummon`) |
| `YgoDuelistCode/Services/FusionSummonSelection.cs` | Fusion targets filtered in `GetFusionTargetsInExtraDeck` (`AllowsSummon`) |
| `YgoDuelistCode/Services/YgoLinkedSpecialSummonSelection.cs` | Linked revive pools: `BuildMonsterCandidates` includes `AllowsSummon` |
| `YgoDuelistCode/Services/YgoGraveyardOptionalDeckSpecialSummon.cs` | Wraps per-card `isCandidate` with `AllowsSummon` |
| `YgoDuelistCode/Services/YgoSpearCretinGraveyard.cs` | GY list filtered |
| `YgoDuelistCode/Services/YgoPinchHopperGraveyard.cs` | Hand list filtered |
| `YgoDuelistCode/Services/YgoLordPoisonGraveyard.cs` | GY list filtered |
| `YgoDuelistCode/Services/YgoGiantGermGraveyard.cs` | Deck list filtered |
| `YgoDuelistCode/Services/YgoMorphingJar2SoloFlip.cs` | Excavate loop gated with `AllowsSummon` |
| `YgoDuelistCode/Services/YgoSarcophagusChain.cs` | `BuildSpiritHandOrDeckCandidates` filtered |

---

## B. Cards / commands with explicit `ReactorSlimeSummonGate` (not blind one-offs)

These card modules mention the gate for **playability or candidate lists**, not only implicit resolve:

| File | Notes |
|------|--------|
| `Cards/Core/NormalMonsterCard.cs` | Normal summon playability under divine-only lock (`GetEffectiveDuelMonsterRace`) |
| `Cards/Spell/Monster_Reborn.cs` | `BuildGraveyardMonsters` uses `SummonCandidatePredicate<BaseMonsterCard>` (same contract as `TrySummon`); no duplicate resolve guard |
| `Cards/Spell/Done/Continuos/Call_of_the_Mummy.cs` | `IsEligibleZombieHandSpecial` mirrors `AllowsSummon` |
| `Cards/Trap/Done/Linked/Metal_Reflect_Slime.cs` | Playability / activation gated when restricted |
| `Cards/Monster/Done/Effect/Reactor_Slime.cs` | Marks restriction; gates activated effects |
| `Cards/Monster/Done/Effect/Spirit_of_the_Pharaoh.cs` | `BuildZombieGraveyardCandidates` includes `AllowsSummon` |

---

## C. Card-local one-offs — `TrySummonDuelMonster*` callsites with **no** `ReactorSlimeSummonGate` in that file

Each row is a **distinct card/command module** that calls `DuelMonsterSummon` directly. Summons still **fail legally** at `DuelMonsterSummon` when the divine-only lock applies; **grid/UI may still offer illegal rows** until a local `Build*Candidates` adds `AllowsSummon` (same pattern as playbook).

Sorted by path:

| # | File |
|---|------|
| 1 | `Cards/Command/Special_Summon_Dark_Sage.cs` |
| 2 | `Cards/Command/Special_Summon_Egyptian_God_Slime.cs` |
| 3 | `Cards/Command/Special_Summon_Union_Fusion_From_Field_Base.cs` |
| 4 | `Cards/Command/Special_Summon_Wall_Shadow.cs` |
| 5 | `Cards/Command/Unequip_Union_Base.cs` |
| 6 | `Cards/Monster/Done/Effect/Ancient_Lamp.cs` |
| 7 | `Cards/Monster/Done/Effect/Aqua_Spirit.cs` |
| 8 | `Cards/Monster/Done/Effect/Black_Luster_Soldier_Envoy_of_the_Beginning.cs` |
| 9 | `Cards/Monster/Done/Effect/Bubonic_Vermin.cs` |
| 10 | `Cards/Monster/Done/Effect/Chaos_Daedalus.cs` |
| 11 | `Cards/Monster/Done/Effect/Chaos_Emperor_Dragon_Envoy_of_the_End.cs` |
| 12 | `Cards/Monster/Done/Effect/Chaos_Sorcerer.cs` |
| 13 | `Cards/Monster/Done/Effect/Cyber_Jar.cs` |
| 14 | `Cards/Monster/Done/Effect/Cyber_Stein.cs` |
| 15 | `Cards/Monster/Done/Effect/D_D_Scout_Plane.cs` |
| 16 | `Cards/Monster/Done/Effect/Dark_Necrofear.cs` |
| 17 | `Cards/Monster/Done/Effect/Despair_from_the_Dark.cs` |
| 18 | `Cards/Monster/Done/Effect/Dice_Jar.cs` |
| 19 | `Cards/Monster/Done/Effect/Don_Turtle.cs` |
| 20 | `Cards/Monster/Done/Effect/Endless_Decay.cs` |
| 21 | `Cards/Monster/Done/Effect/Fear_from_the_Dark.cs` |
| 22 | `Cards/Monster/Done/Effect/Fenrir.cs` |
| 23 | `Cards/Monster/Done/Effect/Fushioh_Richie.cs` |
| 24 | `Cards/Monster/Done/Effect/Garuda_the_Wind_Spirit.cs` |
| 25 | `Cards/Monster/Done/Effect/Ghost_Knight_of_Jackal.cs` |
| 26 | `Cards/Monster/Done/Effect/Gilasaurus.cs` |
| 27 | `Cards/Monster/Done/Effect/Great_Dezard.cs` |
| 28 | `Cards/Monster/Done/Effect/Great_Maju_Garzett.cs` |
| 29 | `Cards/Monster/Done/Effect/Hannibal_Necromancer.cs` |
| 30 | `Cards/Monster/Done/Effect/Lightray_Daedalus.cs` |
| 31 | `Cards/Monster/Done/Effect/Magical_Scientist.cs` |
| 32 | `Cards/Monster/Done/Effect/Maju_Garzett.cs` |
| 33 | `Cards/Monster/Done/Effect/Manticore_of_Darkness.cs` |
| 34 | `Cards/Monster/Done/Effect/Marauding_Captain.cs` |
| 35 | `Cards/Monster/Done/Effect/Nimble_Momonga.cs` |
| 36 | `Cards/Monster/Done/Effect/Ocean_Dragon_Lord_Neo_Daedalus.cs` |
| 37 | `Cards/Monster/Done/Effect/Revival_Jam.cs` |
| 38 | `Cards/Monster/Done/Effect/Skilled_Dark_Magician.cs` |
| 39 | `Cards/Monster/Done/Effect/Skilled_White_Magician.cs` |
| 40 | `Cards/Monster/Done/Effect/Silpheed.cs` |
| 41 | `Cards/Monster/Done/Effect/Soul_of_Purity_and_Light.cs` |
| 42 | `Cards/Monster/Done/Effect/Spirit_of_Flames.cs` |
| 43 | `Cards/Monster/Done/Effect/Summoner_of_Illusions.cs` |
| 44 | `Cards/Monster/Done/Effect/The_Agent_of_Creation_Venus.cs` |
| 45 | `Cards/Monster/Done/Effect/The_Fiend_Megacyber.cs` |
| 46 | `Cards/Monster/Done/Effect/The_Thing_in_the_Crater.cs` |
| 47 | `Cards/Monster/Done/Effect/Twin_Headed_Behemoth.cs` |
| 48 | `Cards/Monster/Done/Effect/Valkyrion_the_Magna_Warrior.cs` |
| 49 | `Cards/Monster/Done/Fusion/The_Last_Warrior_from_Another_Planet.cs` |
| 50 | `Cards/Monster/Done/Ritual/Paladin_of_White_Dragon.cs` |
| 51 | `Cards/Monster/Done/Effect/Gravekeeper_s_Chief.cs` |
| 52 | `Cards/Monster/Done/Effect/Gravekeeper_s_Spy.cs` |
| 53 | `Cards/Spell/Done/Normal/A_Deal_with_Dark_Ruler.cs` |
| 54 | `Cards/Spell/Done/Normal/Contract_with_Exodia.cs` |
| 55 | `Cards/Trap/Done/Linked/Embodiment_of_Apophis.cs` |
| 56 | `Cards/Trap/Done/Linked/The_First_Monarch.cs` |

**Count:** 56 card/command modules (as of repo grep over `YgoDuelistCode/Cards`).

*Exclusions from this table:* `Cards/Core/BaseMonsterCard.cs` and `Cards/Core/AbstractMonsterCard.cs` only contain **XML doc** references to `TrySummonDuelMonster*` (no runtime summon calls).

*Note:* `Paladin_of_White_Dragon` uses a **direct** ritual-adjacent summon path in-card; ritual spell flows through `RitualSummonSelection` separately.

---

## D. Token summon one-offs — `YgoTokenSummon.TrySpecialSummonTokenAsync`

All delegate to `DuelMonsterSummon` via `YgoTokenSummon`. No card file in this list references `ReactorSlimeSummonGate`; resolve gate still applies to token race.

| File |
|------|
| `Cards/Monster/Done/Effect/Cobra_Jar.cs` |
| `Cards/Monster/Done/Effect/Insect_Queen.cs` |
| `Cards/Monster/Done/Effect/Lekunga.cs` |
| `Cards/Monster/Done/Effect/Reactor_Slime.cs` |
| `Cards/Spell/Done/Continuos/Jam_Breeding_Machine.cs` |
| `Cards/Spell/Done/Normal/Multiplication_of_Ants.cs` |
| `Cards/Spell/Done/Normal/Multiply.cs` |
| `Cards/Spell/Done/Normal/Scapegoat.cs` |
| `Cards/Spell/Done/Normal/Stray_Lambs.cs` |
| `Cards/Trap/Done/Normal/Ojama_Trio.cs` |
| `Cards/Trap/Done/Normal/Physical_Double.cs` |
| `Cards/Trap/Done/Normal/Statue_of_the_Wicked.cs` |

**Count:** 12 files.

---

## E. Summary

| Category | Count |
|----------|-------|
| Shared infrastructure (Section A) | 12 paths |
| Cards with explicit gate in-file (Section B) | 6 |
| Card-local `TrySummon*` one-offs without gate string in file (Section C) | 56 |
| Token summon call sites (Section D) | 12 |

**Maintenance:** When tightening divine-lock UX, prefer extending **shared** builders (`YgoPileSearchSelection`, ritual/fusion selection, `YgoOrderedCardSelection` rebuild lambdas) over editing all 56 files; card-local `Where(... AllowsSummon ...)` only where a bespoke candidate LINQ exists.

---

## F. Shared consolidation (post-inventory)

Use these instead of hand-rolling `OrderedCardsOfTypeFromHandDrawDiscard` + `ReactorSlimeSummonGate.AllowsSummon` on every card:

| Helper | Location |
|--------|----------|
| `YgoPlayerPiles.OrderedSummonableMonstersFromHandDrawDiscard<T>()` | Hand + draw + discard union, MP-ordered, filtered by `ReactorSlimeSummonGate` |
| `YgoPlayerPiles.OrderedSummonableMonstersFromPiles<T>(player, pile getters…)` | Arbitrary pile union (e.g. hand + GY + relic grave), same gate |
| `ReactorSlimeSummonGate.SummonCandidatePredicate<TMonster>(player)` | LINQ after `CardsSnapshotOrderedForMp` / `OfType<T>` so hand-built lists match `DuelMonsterSummon` |
| `ReactorSlimeSummonGate.AllowsSummonPrintedRace(player, race)` | Token spells / triggers before a card instance exists; pair with `DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing` when summoning multiple tokens |

**Flip / combat-triggered tokens:** Spells like **`Scapegoat`** already gate in **`IsPlayable`**; monster flip (`Cobra_Jar`) or combat triggers (**`Insect_Queen`**) must still guard **`TrySpecialSummonTokenAsync`** with **`AllowsSummonPrintedRace`** (+ **`HasRoomForDuelSummonAfterReleasing`** where applicable) so UI-less paths do not rely only on resolve-time **`DuelMonsterSummon`**.

**Not convertible:** Trap-card pools typed as `BaseTrapCard` subclasses (e.g. settable `Metal_Reflect_Slime`) — gate is handled via `BlocksNonDivineSummons` / activation guards, not monster pile helpers.

### Batch: `CardsSnapshotOrderedForMp` + `TrySummon` without gate (completed)

Sweep wired **`ReactorSlimeSummonGate`** everywhere the heuristic flagged same-file `CardsSnapshotOrderedForMp` + `OfType<BaseMonsterCard>` (or typed monster) + `TrySummon*`:

- **Summon-from-list:** append **`SummonCandidatePredicate<T>(player)`** (or **`AllowsSummon`** in imperative builders) on the rows that become **`TrySummonDuelMonsterSpecial(player, chosen, …)`** targets — e.g. `Ghost_Knight_of_Jackal`, `Don_Turtle`, `Fushioh_Richie`, `Gravekeeper_s_Spy`, `Gravekeeper_s_Chief`, `Hannibal_Necromancer`, `Dark_Necrofear` (GY/banish summon pool), `Valkyrion_the_Magna_Warrior` (`BuildGraveyardMagnetPool`), **`Monster_Reborn`** (`BuildGraveyardMonsters`).
- **Banish-from-GY cost + summon `this`:** do **not** filter banish fodder with summon predicate; gate **printed race** of **`this`** in **`CanResolveHandSpecialSummon` / `CanMeet*`** via **`AllowsSummonPrintedRace(player, printedRace)`** — e.g. chaos envoys, attribute spirits, **`Lightray_Daedalus`**, **`Manticore_of_Darkness`** (UI-less end phase), **`Ocean_Dragon_Lord_Neo_Daedalus`** hand tribute path.

---

*Generated from exhaustive grep for `TrySummonDuelMonster` / `TrySummonDuelMonsterSpecial` and `YgoTokenSummon.TrySpecialSummonTokenAsync` under `YgoDuelistCode/`.*
