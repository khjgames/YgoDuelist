# BaseMonsterCard stat & energy balancing (design)

How **printed** ATK, DEF, level, and upgrade bonuses relate to **attack-stance** and **defense-stance** play energy for a monster card **on its own** — not other cards, not field effects, not one-off card text.

---

## Smith: printed ATK, DEF, and MGC

Each of ATK, DEF, and MGC upgrades **separately**. The upgrade bump depends only on that line’s **unupgraded** value:

- **+2** if the base is under 15  
- **+3** if the base is 15–21  
- **+4** if the base is 22–28  
- **+5** if the base is 29 or higher  

---

## Play energy from level and Z (unupgraded)

**Level** is the monster’s **printed** level (stars).

**Z** is the **printed** value of the stat that stance uses: **base ATK** for attack stance (summon or play as attack / Command Attack), **base DEF** for defense stance (summon or play as defense / Command Defend). Use values **before** smith upgrades when reading the tables below.

If ATK or DEF is **unknown** (e.g. `?`) or **invalid**, treat that stance’s energy as **1** and skip the efficiency rule in the next section.

### Energy by level and Z

**Normal and Effect monsters (not Ritual, not Fusion), level 1–4**

- Z ≤ 3 → 0 energy  
- Z 4–7 → 1 energy  
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