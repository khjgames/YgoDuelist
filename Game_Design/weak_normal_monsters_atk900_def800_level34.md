# Normal monsters: Level 3–4, ATK under 900, DEF under 800

From `cards_database.json`. Type `Normal Monster`, numeric stats, Level 3–4 inclusive, ATK strictly under 900, DEF strictly under 800. Sorted by `max(ATK, DEF)` descending (then ATK, then name).

**Count:** 7

| max | ATK | DEF | Name | ID |
|-----|-----|-----|------|-----|
| 850 | 850 | 400 | Boneheimer | 98456117 |
| 850 | 850 | 700 | Twin Long Rods #2 | 29692206 |
| 800 | 800 | 500 | Flying Fish | 31987274 |
| 800 | 800 | 700 | Nemuriko | 90963488 |
| 800 | 800 | 700 | The Furious Sea King | 18710707 |
| 800 | 800 | 700 | The Shadow Who Controls the Dark | 63125616 |
| 750 | 350 | 750 | Lightning Conger | 27671321 |

Regenerate: `python filter_weak_normals.py` in this folder.
