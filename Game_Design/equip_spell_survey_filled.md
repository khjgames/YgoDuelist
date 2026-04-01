# Equip spell batch — survey (Step 1)

Working notes for the equip-spell plan. **Per-card rows are draft placeholders** for implementation planning, not answers you signed off on.

**Stat scale (mod units — user rule):** Any TCG printed ATK/DEF bonus that is a multiple of 100 maps **÷ 100** (e.g. TCG **+500** → mod **+5**; TCG **+800** → mod **+8**). Apply the same to per-card equip bonuses in the table unless a card already ships with different numbers in code.

**PvE / duelist-only (user rule):** Slay the Spire 2 is **PvE**. There is no opposing **duelist** with YGO **hand, deck, or graveyard** (or other player piles). Any TCG text about **your opponent’s** hand / deck / GY / banish / “take their card” must be **rewritten to your duelist only**: your piles, your field monsters, your Spell/Trap zone, your LP substitute, etc. Do not implement effects that require a second player’s zones. Cards that only worked by **equipping to the opponent’s monster** or **reviving from the opponent’s GY** need an **Adapted** design on **your** side of the rules (or Skip) — details in **Open**.

**Code status legend:** `Equip` = `BaseEquipSpellCard`; `Placeholder` = still `BaseSpellCard` with empty effect.

---

## Global rules

### Confirmed scaling — Mage Power

TCG: **+500 ATK and +500 DEF** for each Spell/Trap you control. Mod: **+5 ATK and +5 DEF** per counted card (**÷ 100**).

**What to count (implementation draft — confirm or edit):**

- **Include:** Each **face-up** spell/trap you control in the **Spell/Trap zone pile** (`SpellTrapZonePile`), per `YgoSpellTrapZoneBridge.IsSpellOrTrapCard` — equips in zone, continuous S/T, face-up traps.
- **Include Field Spell:** Yes if it sits in that pile face-up.
- **Exclude:** Hand, deck, GY, exhaust; **face-down** sets.
- **Self:** This equip counts itself once it is face-up in the zone (TCG).

Dynamic vars on the card for preview text.

### Confirmed scaling — United We Stand

TCG: **+800 ATK and +800 DEF** for each face-up monster you control **and** each face-up Spell/Trap you control. Mod: **+8 ATK and +8 DEF** per counted card (**÷ 100**).

**What to count (implementation draft — confirm or edit):**

- **Monsters:** Each friendly **field** monster (same source as equip targets); clarify face-up vs face-down if the mod distinguishes.
- **S/T:** Same face-up Spell/Trap count as Mage Power (field spell, equips in zone, etc.).
- **Exclude:** Hand size, deck, GY.
- **No double-count:** A card is either on the monster field or in the S/T zone, not both.

### Open — you decide (not filled in for you)

**Premature Burial:** LP substitute + special summon from **your** discard/GY only (duelist piles), then attach this equip — never “opponent’s” zones. How much cost (HP, energy, discard, exhaust)?

**Autonomous Action Unit:** TCG steals from **opponent’s** GY — **invalid** as written. Redesign: e.g. special summon from **your** GY/discard, or a different self-only effect. What should it do?

**Mask of the Accursed / Paralyzing Potion / Mask of Brutality / Shooting Star Bow:** TCG equips to **opponent’s** monster (or their stats). With **self-only** equips, pick an Adapted pattern: equip **your** monster with a downside to you, convert to a non-equip self spell, **Skip**, or another rule you want (still no opponent duelist zones).

**Magical Labyrinth:** Same **Open** as other rewrites; any stat line uses **÷ 100** vs TCG.

**Scroll of Bewitchment:** Strict attribute change vs stat-only adaptation — `DuelMonsterAttribute` on `BaseMonsterCard` is **card-static** today; future runtime override vs ÷100 combat bonus — your call.

---

## Implementation clarity (1–10)

**What the score means:** How well the TCG effect maps to **existing** equip APIs (`GetEquipStatEffect`, `CanEquipTo`, Splinter/Blight flags, `GraveyardRelic`-style hooks) **and** how much **design is still open** (PvE rewrites, LP, unset riders). **10** = ship or copy a reference card with numbers; **1** = skip or wait for other systems.

| Score | Meaning (short) |
|------:|------------------|
| **10** | Already implemented; no open questions. |
| **9** | Flat ÷100 stats + simple `CanEquipTo` (any, one race, or one attribute); omit minor TCG costs/lines. |
| **8** | Same as 9 plus a small twist: OR-race gate, negative DEF, or creative multiplier **already shipped** (Megamorph). |
| **7** | Obvious mod effect; needs **GrantsSplinterTo** / optional “vs type” bonus or always-on Splinter with a stat oddity (Twin Swords). |
| **6** | Still straightforward code, but **policy** (Insect Armor targets) or **destroy/zone rider** (Deep-Seated), or **count your field monsters** (Opti-Camouflage). |
| **5** | Needs a **new or extended hook**: on-hit threshold, draw-on-damage, stacking ATK on kills, on-kill draw (Baou). |
| **4** | **Dynamic stats** from counting zone + field (Mage Power, UWS) or Labyrinth after you lock **Open** — implementation path is clear once spec is fixed. |
| **3** | **PvE redesign required** (opponent-equip cards, Scroll direction) **or** summon-from-GY + equip + cost (Premature Burial) before it is straightforward. |
| **2** | **No 1:1 translation** until you choose a new identity (AAU, Dragonic Attack). |
| **1** | **Skip / archetype hold** — not worth implementing until a theme exists. |

### All 45 cards, highest clarity first

| Clarity | Card | Notes |
|--------:|------|--------|
| 10 | Big_Bang_Shot | Done. |
| 10 | Black_Pendant | Done. |
| 9 | Axe_of_Despair | Large flat ATK; omit discard cost. |
| 9 | Burning_Spear | Pyro gate; stats only. |
| 9 | Butterfly_Dagger_Elma | Spellcaster; stats only. |
| 9 | Cyber_Shield | Warrior; DEF only. |
| 9 | Elf_S_Light | Spellcaster; stats only. |
| 9 | Gravity_Axe_Grarl | Warrior; stats only. |
| 9 | Gust_Fan | WingedBeast; stats only. |
| 9 | Horn_of_Light | Any; DEF only. |
| 9 | Horn_of_the_Unicorn | Beast; stats only. |
| 9 | Invigoration | Rock; stats only. |
| 9 | Malevolent_Nuzzler | Fiend; stats only. |
| 9 | Rod_of_Silence_Kay_est | Spellcaster; stats only (+ optional small DEF). |
| 9 | Salamandra | Pyro; stats only. |
| 9 | Steel_Shell | Aqua; DEF only. |
| 8 | Cestus_of_Dagla | Spellcaster + DEF on card; heal already wired in relic. |
| 8 | Fusion_Sword_Murasame_Blade | `CanEquipTo` Machine or Dragon. |
| 8 | Lightning_Blade | Warrior; ATK + negative DEF. |
| 8 | Megamorph | Done; multiplier logic is non-trivial but settled. |
| 8 | Shine_Palace | LIGHT attribute gate; symmetric stats. |
| 7 | Burning_Beast | Done; on-hit debuffs via existing relic pattern. |
| 7 | Dragon_Nails | Flat ATK + Splinter only if Dragon. |
| 7 | Sword_of_Dark_Destruction | DARK gate + optional vs-Fairy line. |
| 7 | Sword_of_Dragon_S_Soul | Warrior + optional vs-Dragon line. |
| 7 | Twin_Swords_of_Flashing_Light_Tryce | Negative ATK + always Splinter (clear metaphor). |
| 6 | Insect_Armor_with_Laser_Cannon | Multi-race `CanEquipTo` + conditional Splinter; **typing policy** still to settle. |
| 6 | Opti_Camouflage_Armor | Base stats + **your** field monster count for extra bonus. |
| 6 | Sword_of_Deep_Seated | Stats + “returns to deck” rider (Ethereal-like) if you want it. |
| 5 | Buster_Rancher | Low printed ATK threshold + on-hit rider (pick one). |
| 5 | Rod_of_the_Mind_s_Eye | Stats + first-time draw on unblocked damage. |
| 5 | Sword_of_the_Soul_Eater | Static stats easy; stacking per kill needs state. |
| 5 | Wicked_Breaking_Flamberge_Baou | Stats + on-kill draw (self-only). |
| 4 | Magical_Labyrinth | Fish/Aqua + **Open** flavor; once locked, mostly stats/conditional. |
| 4 | Mage_Power | Count face-up S/T → `GetEquipStatEffect`; needs small helper + refactor off placeholder. |
| 4 | United_We_Stand | Count field monsters + S/T; same helper story as Mage Power. |
| 3 | Mask_of_Brutality | Opponent-equip → **self** redesign unset. |
| 3 | Mask_of_the_Accursed | Same. |
| 3 | Paralyzing_Potion | Same. |
| 3 | Premature_Burial | GY/discard summon + attach equip + **Open** cost; Call of Haunted–style glue. |
| 3 | Scroll_of_Bewitchment | **Open** (attribute rewrite vs combat bonus). |
| 3 | Shooting_Star_Bow_Ceal | Opponent-equip → **Open** self redesign. |
| 2 | Autonomous_Action_Unit | TCG identity invalid in PvE; needs new effect you choose. |
| 2 | Dragonic_Attack | Opponent field / control — no direct port. |
| 1 | Card_7_Completed | Archetype / skip until Seven Tools is a real pillar. |

### Quick buckets (same order: do high scores first)

- **10:** Big_Bang_Shot, Black_Pendant  
- **9:** Axe_of_Despair, Burning_Spear, Butterfly_Dagger_Elma, Cyber_Shield, Elf_S_Light, Gravity_Axe_Grarl, Gust_Fan, Horn_of_Light, Horn_of_the_Unicorn, Invigoration, Malevolent_Nuzzler, Rod_of_Silence_Kay_est, Salamandra, Steel_Shell  
- **8:** Cestus_of_Dagla, Fusion_Sword_Murasame_Blade, Lightning_Blade, Megamorph, Shine_Palace  
- **7:** Burning_Beast, Dragon_Nails, Sword_of_Dark_Destruction, Sword_of_Dragon_S_Soul, Twin_Swords_of_Flashing_Light_Tryce  
- **6:** Insect_Armor_with_Laser_Cannon, Opti_Camouflage_Armor, Sword_of_Deep_Seated  
- **5:** Buster_Rancher, Rod_of_the_Mind_s_Eye, Sword_of_the_Soul_Eater, Wicked_Breaking_Flamberge_Baou  
- **4:** Magical_Labyrinth, Mage_Power, United_We_Stand  
- **3:** Mask_of_Brutality, Mask_of_the_Accursed, Paralyzing_Potion, Premature_Burial, Scroll_of_Bewitchment, Shooting_Star_Bow_Ceal  
- **2:** Autonomous_Action_Unit, Dragonic_Attack  
- **1:** Card_7_Completed  

---

## Per-card survey (all 45 files under `Cards/Spell/Todo/Equip`)

Columns: **Fidelity** = Strict / Adapted / Skip | **Restriction** | **ATK/DEF** (mod units, base) | **Upg** = upgrade delta suggestion | **Mult** | **Spl** = Splinter | **Blt** = Blight | **Other**

| # | Card | Fidelity | Restriction | ATK | DEF | Upg | Mult | Spl | Blt | Other |
|---|------|----------|-------------|-----|-----|-----|------|-----|-----|--------|
| 1 | Axe_of_Despair | Adapted | Any | 10 | 0 | +2/+0 | — | — | — | TCG discard cost → omit; stats only. |
| 2 | Autonomous_Action_Unit | Adapted | Any | 0 | 0 | — | — | — | — | No opponent GY — redesign on **your** piles only; LP substitute **Open**. Placeholder. |
| 3 | Big_Bang_Shot | Strict | Any | 4 | 0 | +4/+0 | — | Yes | — | Implemented. |
| 4 | Black_Pendant | Strict | Any | 5 | 0 | +3/+0 | — | — | Yes | Implemented. |
| 5 | Burning_Beast | Adapted | Any | 0 | 0 | — | — | — | — | On-hit Weak+Vuln (GraveyardRelic). Implemented. |
| 6 | Burning_Spear | Strict | Pyro | 7 | 0 | +2/+0 | — | — | — | Fiend-only in some prints — use **Pyro** only to match name. |
| 7 | Buster_Rancher | Adapted | Any | 3 | 0 | +2/+0 | — | — | — | If equipped **base ATK ≤ 2** (mod scale), attacks **+2 damage** or auto **Blight 2** on hit — pick one implementation. |
| 8 | Butterfly_Dagger_Elma | Strict | Spellcaster | 3 | 0 | +2/+0 | — | — | — | Banned-card joke OK to keep as normal uncommon. |
| 9 | Card_7_Completed | Skip | — | — | — | — | — | — | — | Archetype-specific / rule mess; leave stub or remove from packs until Seven-Tools theme exists. |
| 10 | Cestus_of_Dagla | Adapted | Spellcaster | 0 | 5 | +0/+2 | — | — | — | Add **+5 DEF** (TCG); keep heal-on-hit via Mgc var if desired. Implemented partial. |
| 11 | Cyber_Shield | Adapted | Warrior | 0 | 5 | +0/+2 | — | — | — | TCG “female” → **Warrior** only. |
| 12 | Dragon_Nails | Strict | Any (+Spl on Dragon) | 6 | 0 | +2/+0 | — | Conditional | — | Splinter if **Dragon**; add flat ATK (TCG +600). |
| 13 | Dragonic_Attack | Skip | — | — | — | — | — | — | — | TCG uses opponent’s field — no PvE opponent duelist; **Skip** or full Adapted self-only redesign **Open**. |
| 14 | Elf_S_Light | Strict | Spellcaster | 3 | 0 | +2/+0 | — | — | — | Battle damage effect → omit. |
| 15 | Fusion_Sword_Murasame_Blade | Strict | Machine **or** Dragon | 7 | 0 | +2/+0 | — | — | — | `CanEquipTo` either race. |
| 16 | Gravity_Axe_Grarl | Adapted | Warrior | 5 | 0 | +2/+0 | — | — | — | Position lock → omit. |
| 17 | Gust_Fan | Strict | WingedBeast | 8 | 0 | +2/+0 | — | — | — | WIND battle bonus → omit or future hook. |
| 18 | Horn_of_Light | Strict | Any | 0 | 8 | +0/+2 | — | — | — | |
| 19 | Horn_of_the_Unicorn | Strict | Beast | 7 | 0 | +2/+0 | — | — | — | Return to hand on destroy → omit. |
| 20 | Insect_Armor_with_Laser_Cannon | Strict | Insect **or** Beast **or** BeastWarrior **or** WingedBeast | 7 | 0 | +2/+0 | — | Conditional | — | Splinter only for **Beast / BeastWarrior / WingedBeast** (current code); **Insect** gets **only** stats (TCG Insect equip). Adjust `CanEquipTo` to **Insect-only** if you want strict TCG typing. |
| 21 | Invigoration | Strict | Rock | 10 | 0 | +2/+0 | — | — | — | TCG EARTH Rock — use **Rock** race. |
| 22 | Lightning_Blade | Strict | Warrior | 8 | -5 | +2/-1 | — | — | — | “No Thunder on field” → omit. |
| 23 | Magical_Labyrinth | Adapted | Fish **or** Aqua | 0 | 0 | — | — | — | — | See **Open** section; stats **÷ 100** if you add a TCG mirror. |
| 24 | Mage_Power | Strict | Any | dyn | dyn | scale | — | — | — | See global count rules; `GetEquipStatEffect` from count. Placeholder. |
| 25 | Malevolent_Nuzzler | Strict | Fiend | 6 | 0 | +2/+0 | — | — | — | Discard cost → omit. |
| 26 | Mask_of_Brutality | Adapted | **Self** redesign | 10 | -5 | +2/-1 | — | — | — | TCG equips to opponent’s monster — **Open** (see global PvE rule). |
| 27 | Mask_of_the_Accursed | Adapted | **Self** redesign | 0 | 0 | — | — | — | — | Same — **Open**; no opponent-monster equip. Placeholder. |
| 28 | Megamorph | Adapted | Any | — | — | cost | Yes | — | — | Implemented (HP% multiplier). |
| 29 | Opti_Camouflage_Armor | Adapted | Any | 2 | 2 | +1/+1 | — | — | — | Draft; solo-monster clause: extra bonus should follow **÷ 100** vs TCG if you assign a printed value. |
| 30 | Paralyzing_Potion | Adapted | **Self** redesign | 0 | 0 | — | — | — | — | Same — **Open**. Placeholder. |
| 31 | Premature_Burial | Adapted | Any | 0 | 0 | — | — | — | — | Summon from **your** GY/discard + equip; LP substitute **Open**. Placeholder. |
| 32 | Rod_of_Silence_Kay_est | Adapted | Spellcaster | 4 | 0 | +2/+0 | — | — | — | Optional extra DEF: **÷ 100** vs any TCG bonus you pick. |
| 33 | Rod_of_the_Mind_s_Eye | Adapted | Spellcaster | 3 | 0 | +2/+0 | — | — | — | Draw on battle damage → **draw 1** once when equipped monster first deals unblocked damage (Phase 4 hook) or skip. |
| 34 | Salamandra | Strict | Pyro (or FIRE attribute if preferred) | 7 | 0 | +2/+0 | — | — | — | Use **Pyro** race gate to avoid attribute branch. |
| 35 | Scroll_of_Bewitchment | Adapted | Any | 0 | 0 | — | — | — | — | See **Open** (Scroll / attribute). Placeholder. |
| 36 | Shooting_Star_Bow_Ceal | Adapted | **Self** redesign | — | — | — | — | — | — | TCG equips to opponent’s monster — no that pipeline; **Open** (e.g. self **Warrior** stat line **÷ 100** or Skip). |
| 37 | Shine_Palace | Strict | LIGHT attribute | 7 | 7 | +1/+1 | — | — | — | Gate: `DuelMonsterAttribute.Light`. |
| 38 | Steel_Shell | Strict | Aqua | 0 | 4 | +0/+2 | — | — | — | |
| 39 | Sword_of_Dark_Destruction | Adapted | DARK attribute | 5 | 0 | +2/+0 | — | — | — | Optional vs-Fairy line: use **÷ 100** vs TCG bonus (e.g. +300 → +3). |
| 40 | Sword_of_Dragon_S_Soul | Adapted | Warrior | 5 | 0 | +2/+0 | — | — | — | Optional vs-Dragon line: **÷ 100** vs TCG bonus. |
| 41 | Sword_of_Deep_Seated | Adapted | Any | 5 | 0 | +2/+0 | — | — | — | Return to deck on destroy → **Ethereal**-style or omit. |
| 42 | Sword_of_the_Soul_Eater | Adapted | Any | 6 | 0 | +2/+0 | — | — | — | Gains ATK from destroyed monsters — if implemented as stacks, use **÷ 100** vs intended TCG step per kill (e.g. +200 ATK → +2 per kill). |
| 43 | Twin_Swords_of_Flashing_Light_Tryce | Adapted | Any | -5 | 0 | — | — | Yes | — | “Two attacks” → **Splinter** represents multi-hit spread; keep **-5 ATK** flat. |
| 44 | United_We_Stand | Strict | Any | dyn | dyn | scale | — | — | — | See global. Placeholder. |
| 45 | Wicked_Breaking_Flamberge_Baou | Adapted | Any | 5 | 0 | +2/+0 | — | — | — | On-kill rider: **your** duelist benefit only (e.g. draw 1); no opponent duelist zones. |

---

## Notes for implementation order (from plan)

- **Phase 1:** Rows with numeric ATK/DEF only and `CanEquipTo` true or simple race/attribute gates.
- **Phase 2:** Conditional Splinter (Dragon Nails, Insect Armor policy), Twin Swords, Malevolent Nuzzler, etc.
- **Phase 3:** Mage Power, United We Stand (shared counter helper on owner + zone).
- **Phase 4:** Burning Beast-style registry for rods, Baou, Buster Rancher; any mask-style riders after **self-only** redesign.
- **Phase 5:** Premature Burial / AAU from **your** piles; opponent-monster equips replaced per **Open** (no enemy duelist zones).

---

*Per-card table: draft for pacing; replace any row with your own answers. LP / mask / scroll / labyrinth rows need your decisions in the **Open** section above.*
