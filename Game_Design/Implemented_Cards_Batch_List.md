# Effect monsters — integration batch list (Uncommon / BundledCards triage)

Use this list to mark which cards should be **Uncommon** and which need **BundledCards**.  
Code column: **Done** = moved to `YgoDuelistCode/Cards/Monster/Done/Effect` with behavior; **Todo** = still only stub in `Todo/Effect` or not started; **Partial** = infra only or needs your rules clarification.

| Card | C# | Notes for you (Uncommon? / Bundle?) |
|------|----|--------------------------------------|
| Bite Shoes | Todo | FLIP battle position — not migrated this batch |
| Gren Maju Da Eiza | Todo | Banished × Mgc ATK/DEF — not migrated |
| Fiber Jar | Todo | Field reset — needs your solo/coop scope confirmation |
| Morphing Jar | Todo | Hand discard + draw — not migrated |
| Morphing Jar #2 | Todo | Field shuffle — not migrated |
| Ryu Kishin Clown | Todo | On Summon position change — not migrated |
| Witch Doctor of Chaos | Todo | FLIP banish from GY (opponent GY N/A in SP) — clarify |
| Birdface | Todo | |
| Breaker the Magical Warrior | Todo | Spell counters — pattern exists (`IYgoSpellCounterMonster`) |
| Boar Soldier | Todo | Normal summon destroy / ATK −10 — clarify vs tribute-only |
| Lady Panther | Todo | Tribute + return battle-destroyed — needs battle-loss registry |
| Mataza the Zapper | Todo | Second attack (`AttackPortionCount` 2) |
| Marauding Captain | Todo | “Cannot attack other Warriors” — **no precedent**; needs your StS mapping |
| Makyura the Destructor | Todo | Trap from hand same turn — **needs your flow** |
| Mermaid Knight | Todo | Double attack with Umi — straightforward |
| Mirage Knight | Todo | Cannot Normal Summon — gate only |
| Penguin Knight | Todo | Deck→GY shuffle draw+GY — heavy; not migrated |
| Magical Merchant | Todo | FLIP excavate — not migrated |
| Banisher of the Light | **Done** | Global GY→Banish while face-up (`YgoBanisherOfLightRules` + patch) |
| Troop Dragon | Todo | Battle death deck SS self — same pattern as Shining Angel |
| Wall Shadow | Todo | Requires Magical Labyrinth — **no card in repo**; gate only or bundle later |
| Mucus Yolk | Todo | 50% Blight on attacks — one-liner when migrated |
| Gear Golem the Moving Fortress | Todo | Activated HP for full Blight — `MonsterCommandState.GearGolemFullBlightAttacksThisTurn` added |
| Ocean Dragon Lord Neo Daedalus | Done | (reference only) |
| Ghost Knight of Jackal | Todo | Execute → optional GY SS — not migrated |
| Kycoo the Ghost Destroyer | Todo | Attack opening banish 0–2 from GY — not migrated |
| Mobius the Frost Monarch | Todo | Tribute ST destroy + Magic Plating — not migrated |
| Mystical Knight of Jackal | Todo | Execute → top-deck 1 from GY — not migrated |
| Needle Burrower | Todo | Corpse-Blight only — copy `Des_Volstgalph` |
| Newdoria | Todo | Battle destroy optional kill own + Conduit/Energy |
| Nimble Momonga | Todo | Battle heal + deck SS face-down ×2 |
| Pixie Knight | Todo | Battle destroy spell to top of draw |
| Revival Jam | Todo | Battle destroy pay HP revive DEF |
| Sacred Crane | Todo | On Summon draw 1 |
| The Agent of Creation - Venus | Todo | Activate pay HP SS Mystical Shine Ball |
| UFO Turtle | Todo | Same as Shining Angel line (Fire ≤ ATK) |
| Winged Sage Falcos | Todo | Execute → monster top deck |
| Archfiend of Gilfer | Todo | GY → Gilfer Power on chosen monster (`GilferPowerPower` + stat hook **added**) |
| Despair from the Dark | Todo | GY tribute 2 SS self |
| Harpie's Pet Dragon | Todo | ATK/DEF per Harpie Lady |
| Roulette Barrel | Todo | Activate roll d6 damage |
| Witch of the Black Forest | Todo | Field→GY search DEF ≤15 |
| Zolga | Todo | Tribute heal Mgc |

## Infra added this session (shared)

- `GilferPowerPower` + `powers.json` + `BaseMonsterCard` ATK/DEF integration for Gilfer stacks.
- `MonsterCommandState.GearGolemFullBlightAttacksThisTurn` (+ cleared in `ClearPerTurnExtrasForPlayer`) for Gear Golem when you implement it.
- `YgoBanisherOfLightRules` + `CardPileCmdBanisherOfLightGyRedirectPatch` + `Banisher_of_the_Light` in **Done** (removed Todo duplicate).

## Clarifications still needed from you

1. **Fiber Jar / Morphing Jar (both players)** — confirm owner-only vs all combat players for hand/field/GY in co-op.
2. **Witch Doctor of Chaos** — opponent Graveyard: banish from **your** GY only, or skip in SP?
3. **Marauding Captain** — how enemy targeting should work vs Warrior pets (intent system).
4. **Makyura** — exact StS equivalent for “activate 1 Trap from hand this turn.”
5. **Wall Shadow** — until **Magical Labyrinth** exists, keep unsummonable only, or temporary alternate summon condition?

Close **SlayTheSpire2.exe** before building so the DLL copy step succeeds.
