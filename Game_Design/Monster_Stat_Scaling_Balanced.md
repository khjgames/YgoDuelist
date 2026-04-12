# BaseMonsterCard stat & energy balancing (design)

How **printed** ATK, DEF, level, and upgrade bonuses relate to **attack-stance** and **defense-stance** play energy for a monster card **on its own** — not other cards, not field effects, not one-off card text.

---

## Smith: printed ATK, DEF, and MGC

Each of ATK, DEF, and MGC upgrades **separately**. The **first upgrade** maps **unupgraded** printed **energy cost** and that line’s **unupgraded** value to new cost and value — **not** a single global “+N by threshold” rule.

**Effect** monsters **level 4 and below** keep the **previous** Effect smith scaling (unchanged). The tables below apply to **Normal** (where noted), **Ritual**, **Fusion**, and **Effect** at **level 5+** as indicated.

**Ritual** and **Fusion** use the **Ritual / Fusion** rows. **Normal / Effect** (and **Normal / Effect / Ritual** where listed) use the matching combined rows.

**MGC** uses the **same mapping as ATK** for the same monster category and level band (read **base MGC** wherever **ATK** appears in the tables).

**DEF** uses the **same rules as ATK**, but the **Z band is 1 lower** (e.g. where ATK uses “2 cost 15,” the parallel DEF row is “2 cost 14,” etc.). Apply that offset to every explicit ATK row when resolving DEF.

### ATK — Normal, level 4 and below

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 4 | → | 0 | 7 |
| 1 | 7 | → | 1 | 9 |
| 1 | 8 | → | 1 | 10 |
| 2 | 9 | → | 1 | 14 |
| 2 | 10–12 | → | 1 | 13 |
| 2 | 13–14 | → | 2 | 16 |
| 2 | 15–16 | → | 2 | 19 |
| 2 | 17 | → | 2 | 20 |
| 2 | 18–21 | → | +3 ATK (same cost rules as band above) |
| 2 | 22+ | → | +4 ATK |

### ATK — Ritual / Fusion, level 4 and below

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 11 | → | 1 | 14 |
| 1 | 12 | → | 1 | 15 |
| 2 | 13 | → | 1 | 19 |
| 2 | 14–16 | → | 1 | 18 |
| 2 | 17–18 | → | 2 | 21 |
| 2 | 19–20 | → | 2 | 24 |
| 2 | 21 | → | 2 | 25 |
| 2 | 22–25 | → | +4 ATK |
| 2 | 26+ | → | +5 ATK |

### ATK — Normal / Effect, level 5–6

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 12 | → | 1 | 15 |
| 1 | 13 | → | 1 | 16 |
| 2 | 14 | → | 1 | 20 |
| 2 | 15–17 | → | 1 | 19 |
| 2 | 18–19 | → | 2 | 22 |
| 2 | 20–21 | → | 2 | 25 |
| 2 | 22 | → | 2 | 26 |
| 2 | 23–26 | → | +4 ATK |
| 2 | 27+ | → | +5 ATK |

### ATK — Ritual / Fusion, level 5–6

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 13 | → | 1 | 17 |
| 1 | 14 | → | 1 | 18 |
| 2 | 15 | → | 1 | 22 |
| 2 | 16–18 | → | 1 | 21 |
| 2 | 19–20 | → | 2 | 24 |
| 2 | 21–22 | → | 2 | 27 |
| 2 | 23 | → | 2 | 28 |
| 2 | 24–27 | → | +5 ATK |
| 2 | 28+ | → | +6 ATK |

### ATK — Normal / Effect / Ritual, level 7–8

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 16 | → | 1 | 20 |
| 1 | 17 | → | 1 | 21 |
| 2 | 18 | → | 1 | 25 |
| 2 | 19–21 | → | 1 | 24 |
| 2 | 22–23 | → | 2 | 27 |
| 2 | 24–25 | → | 2 | 30 |
| 2 | 26 | → | 2 | 31 |
| 2 | 27–30 | → | +5 ATK |
| 2 | 31+ | → | +6 ATK |

### ATK — Fusion, level 7–8

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 18 | → | 1 | 23 |
| 1 | 19 | → | 1 | 24 |
| 2 | 20 | → | 1 | 28 |
| 2 | 21–23 | → | 1 | 27 |
| 2 | 24–25 | → | 2 | 30 |
| 2 | 26–27 | → | 2 | 33 |
| 2 | 28 | → | 2 | 34 |
| 2 | 29–32 | → | +6 ATK |
| 2 | 33+ | → | +7 ATK |

### ATK — BaseMonsterCard (all types), level 9–10

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 25 | → | 1 | 30 |
| 1 | 26 | → | 1 | 31 |
| 2 | 27 | → | 1 | 35 |
| 2 | 28–30 | → | 1 | 34 |
| 2 | 31–32 | → | 2 | 37 |
| 2 | 33–34 | → | 2 | 40 |
| 2 | 35 | → | 2 | 41 |
| 2 | 36–39 | → | +6 ATK |
| 2 | 40+ | → | +7 ATK |

### ATK — BaseMonsterCard (all types), level 11+

| Unupgraded cost | Unupgraded ATK | → | Upgraded cost | Upgraded ATK |
|-----------------|------------------|---|---------------|--------------|
| 1 | 29 | → | 1 | 34 |
| 1 | 30 | → | 1 | 35 |
| 2 | 31 | → | 1 | 39 |
| 2 | 32–34 | → | 1 | 38 |
| 2 | 35–36 | → | 2 | 41 |
| 2 | 37–38 | → | 2 | 44 |
| 2 | 39 | → | 2 | 45 |
| 2 | 40–43 | → | +6 ATK |
| 2 | 44+ | → | +7 ATK |

### DEF (level 4 and below, Normal — illustrative)

Same mapping as ATK with **DEF 1 lower** than the ATK row for each band (see source: *Normal_Tierlist_Cost_Benefit_Analysis*). Example for **Normal, level ≤4**:

| Unupgraded cost | Unupgraded DEF | → | Upgraded cost | Upgraded DEF |
|-----------------|------------------|---|---------------|--------------|
| 1 | 4 | → | 0 | 7 |
| 1 | 7 | → | 1 | 9 |
| 2 | 8 | → | 1 | 13 |
| 2 | 9–11 | → | 1 | 12 |
| 2 | 12–13 | → | 2 | 15 |
| 2 | 14–15 | → | 2 | 18 |
| 2 | 16 | → | 2 | 19 |
| 2 | 17–21 | → | +3 DEF |
| 2 | 22+ | → | +4 DEF |

Other level bands: apply the **same −1 shift** to the ATK tables above for that category and level.

---

## Play energy from level and Z (unupgraded)

**Level** is the monster’s **printed** level (stars).

**Z** is the **printed** value of the stat that stance uses: **base ATK** for attack stance (summon or play as attack / Command Attack), **base DEF** for defense stance (summon or play as defense / Command Defend). Use values **before** smith upgrades when reading the tables below.

If ATK or DEF is **unknown** (e.g. `?`) or **invalid**, treat that stance’s energy as **1** and skip the efficiency rule in the next section.

### Energy by level and Z

**Normal and Effect monsters (not Ritual, not Fusion), level 1–4**

- Z ≤ 3 → 0 energy  
- Z 4 → 1 energy (onUpgrade: becomes 0 energy)  
- Z 5–7 → 1 energy  
- Z 8 → 1 energy (attack stance), 2 energy (defense stance) (onUpgrade: becomes 1 energy)  
- Z 9–12 → 2 energy (onUpgrade: becomes 1 energy)
- Z 13–20 → 2 energy  
- Z ≥ 21 → 3 energy  

**Ritual or Fusion monsters, level 1–4**

- Z ≤ 7 → 0 energy  
- Z 8–11 → 1 energy  
- Z 12 → 1 energy (attack stance), 2 energy (defense stance) (onUpgrade: becomes 1 energy)  
- Z 13–16 → 2 energy (onUpgrade: becomes 1 energy)
- Z 17–24 → 2 energy  
- Z ≥ 25 → 3 energy  

**Normal and Effect monsters (not Ritual, not Fusion), level 5–6**

- Z ≤ 8 → 0 energy  
- Z 9–12 → 1 energy  
- Z 13 → 1 energy (attack stance), 2 energy (defense stance) (onUpgrade: becomes 1 energy)  
- Z 14–17 → 2 energy (onUpgrade: becomes 1 energy)
- Z 18–25 → 2 energy  
- Z ≥ 26 → 3 energy  

**Ritual or Fusion monsters, level 5–6**

- Z ≤ 9 → 0 energy  
- Z 10–13 → 1 energy  
- Z 14 → 1 energy (attack stance), 2 energy (defense stance) (onUpgrade: becomes 1 energy)  
- Z 15–18 → 2 energy (onUpgrade: becomes 1 energy)
- Z 19–27 → 2 energy  
- Z ≥ 28 → 3 energy  

**Normal, Ritual and Effect monsters (not Fusion), level 7–8**

- Z ≤ 10 → 0 energy  
- Z 11–16 → 1 energy  
- Z 17 → 1 energy (attack stance), 2 energy (defense stance) (onUpgrade: becomes 1 energy) 
- Z 18–22 → 2 energy (onUpgrade: becomes 1 energy)
- Z 23–30 → 2 energy  
- Z ≥ 31 → 3 energy  

**Fusion monsters, level 7–8**

- Z ≤ 11 → 0 energy  
- Z 12–18 → 1 energy  
- Z 19 → 1 energy (attack stance), 2 energy (defense stance) (onUpgrade: becomes 1 energy) 
- Z 20–24 → 2 energy (onUpgrade: becomes 1 energy)
- Z 25–31 → 2 energy  
- Z ≥ 32 → 3 energy  

**Level 9–10**

- Z ≤ 11 → 0 energy  
- Z 12–25 → 1 energy  
- Z 26 → 1 energy (attack stance), 2 energy (defense stance) (onUpgrade: becomes 1 energy) 
- Z 27–32 → 2 energy (onUpgrade: becomes 1 energy)
- Z 33–45 → 2 energy  
- Z ≥ 46 → 3 energy  

**Level 11+**

- Z ≤ 12 → 0 energy  
- Z 13–30 → 1 energy  
- Z 31–35 → 2 energy (onUpgrade: becomes 1 energy)
- Z 36–60 → 2 energy  
- Z ≥ 61 → 3 energy  

---