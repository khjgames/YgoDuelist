# Card localization: keys and dynamic variables

## Canonical `cards.json` key shape

- **Mod cards:** `YGODUELIST-<CARD_CLASS_NAME>.<field>`  
  Example: `YGODUELIST-MUKA_MUKA.description_combat`  
  `<CARD_CLASS_NAME>` is the C# class name in UPPER_SNAKE (same as STS card id entry for YgoDuelist cards).
- **Legacy unprefixed keys** (`MUKA_MUKA.*`, etc.) were removed when identical to the `YGODUELIST-*` pair (see `tools/dedupe_cards_json.py`). Do not reintroduce duplicates.

## Monster cards (`NormalMonsterCard` / `EffectMonsterCard` / …)

| Placeholder | Source |
|-------------|--------|
| `Stars` | `StarsVar` — star cost |
| `Damage` | Printed ATK (`DamageVar`) |
| `Block` | Printed DEF as block (`BlockVar`) |
| `Def` | Printed DEF (duplicate for copy lines) |
| `Mgc` | Secondary stat; hand-scaling multiplier (Muka Muka), WATER bonus (Star Boy), etc. |
| `CalculatedATK` | `ComputedDecimalVar` — ATK after field auras + `GetSecondaryStats` |
| `CalculatedDEF` | Same for DEF |

**Minimum description keys** (toggle attack vs defense stance + combat preview):

- `.title`
- `.description` — out-of-combat / non-combat attack mode
- `.description_combat` — combat attack mode (use `CalculatedATK` / `CalculatedDEF` where totals matter)
- `.description_skill` — defense mode, base printed stats
- `.description_skill_combat` — defense mode in combat

**Patterns**

- **Hand scaling (Muka Muka):** no extra `DynamicVar` names; effect is in `GetSecondaryStats`; text uses `{Mgc}` per other card in hand.
- **Asymmetric field aura (Star Boy, Bladefly, Hoshiningen, Little Chimera, Milus Radiant, Witchs_Apprentice):** add `Mgc2` in `CanonicalVars` for the debuffed attribute; use `loses [blue]{Mgc2}[/blue] ATK` (or equivalent) in all four description keys — never hardcode the old “4” so upgrades stay in sync.

## Spells (`BaseSpellCard` and derived)

Use whatever `CanonicalVars` the card defines — common STS names include `MagicNumber`, `Damage`, `Block`, `Cards`, etc. **Every** placeholder in `cards.json` must exist on that card’s `CanonicalVars`.

## Traps (`BaseTrapCard` and derived)

Same rule as spells: match `CanonicalVars` names to `{Name}` in text.

## Powers / UI

Some cards reference separate keys (e.g. `YGODUELIST-AMEBA.activated_effect.description`). Keep those under the same `YGODUELIST-` prefix.

## Tooling

| Script | Purpose |
|--------|---------|
| `tools/card_inventory_scan.py` | Emit `docs/card_inventory_data.json` |
| `tools/generate_card_inventory_md.py` | Emit `docs/IMPLEMENTED_CARDS.md` |
| `tools/dedupe_cards_json.py` | Drop redundant unprefixed keys when `YGODUELIST-*` matches |
