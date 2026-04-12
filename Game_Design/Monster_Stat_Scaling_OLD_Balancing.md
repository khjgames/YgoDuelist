# BaseMonsterCard stat & energy balancing (design)

How **printed** ATK, DEF, level, and upgrade bonuses relate to **attack-stance** and **defense-stance** play energy for a monster card **on its own** — not other cards, not field effects, not one-off card text.

Below is **unupgraded** play energy. Use **Z** = printed ATK when reading attack stance, **Z** = printed DEF for defense stance. Lines that call out both stances bake in the low-level “efficiency” bump where the two sides differ.

---

## Smith: printed ATK, DEF, and MGC

Each of ATK, DEF, and MGC upgrades **separately**. The upgrade bump depends only on that line’s **unupgraded** value:

- **+2** if the base is under 15  
- **+3** if the base is 15–21  
- **+4** if the base is 22–28  
- **+5** if the base is 29 or higher  

---

## Play energy from level and Z (unupgraded)

**Z** uses the printed stat for the stance you care about, **before** smith upgrades.

If ATK or DEF is **unknown** (e.g. `?`) or **invalid**, that stance’s energy is **1**.

### Normal and effect monsters (not Ritual, not Fusion)

**Printed level 1–4**

- Z ≤ 3 → 0 energy  
- Z 4–7 → 1 energy  
- Z 8 → 1 energy (attack stance), 2 energy (defense stance)  
- Z 9–11 → 2 energy  
- Z 12–20 → 2 energy  
- Z ≥ 21 → 3 energy  

### Ritual or Fusion monsters, printed level 1–4

- Z ≤ 6 → 0 energy  
- Z 7 → 1 energy  
- Z 8 → 1 energy (attack stance), 2 energy (defense stance)  
- Z 9–14 → 2 energy  
- Z 15–23 → 2 energy  
- Z ≥ 24 → 3 energy  

### Printed level 5–6 (all monster types)

- Z ≤ 8 → 0 energy  
- Z 9–12 → 1 energy  
- Z 13 → 1 energy (attack stance), 2 energy (defense stance)  
- Z 14–16 → 2 energy  
- Z 17–25 → 2 energy  
- Z ≥ 26 → 3 energy  

### Printed level 7–8

- Z ≤ 10 → 0 energy  
- Z 11–17 → 1 energy  
- Z 18–30 → 2 energy  
- Z ≥ 31 → 3 energy  

### Printed level 9–10

- Z ≤ 11 → 0 energy  
- Z 12–25 → 1 energy  
- Z 26–45 → 2 energy  
- Z ≥ 46 → 3 energy  

### Printed level 11+

- Z ≤ 12 → 0 energy  
- Z 13–29 → 1 energy  
- Z 30–60 → 2 energy  
- Z ≥ 61 → 3 energy  
