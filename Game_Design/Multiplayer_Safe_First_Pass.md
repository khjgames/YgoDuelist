# MultiplayerSafe first pass (declared `PackTags` only)

## Removed (no runtime augmentation)

- `YgoDuelistCode/Services/YgoMultiplayerSafePackRules.cs` (deleted)
- `YgoDuelistCode/Services/YgoMultiplayerSafeExplicitPicks.Generated.cs` (deleted)
- `YgoPackCardCatalog.GetEffectivePackTags`: no longer ORs `MultiplayerSafe` from procedural rules.

Multiplayer procedural pools still require effective `MultiplayerSafe`. That tag must appear on `YgoDuelistCard.PackTags` unless one of the exceptions below applies.

## Implicit effective tags (unchanged)

`GetEffectivePackTags` still:

1. ORs `MultiplayerSafe` when declared tags are **`None`** and the template is **`Strike_YgoDuelist`**, **`Defend_YgoDuelist`**, or **`BaseYgoPowerCard`** (potion / library keep declared `None`).
2. Adds fusion-material profile tags when `FusionMaterialArchetypeIndex.IsNamedFusionMaterial` applies (`Fusion`, attribute/race, `Normal` / `Ritual` as before).

## How `MultiplayerSafe` was applied in source

1. **Central bases (single edit sites)**  
   - All **`FusionMonsterCard`**: `FusionMonsterCard.PackTags` leads with `MultiplayerSafe`.  
   - **Named fusion materials** (normal / effect / ritual monsters listed as `typeof(...)` in a `FusionMonsterCard` recipe): `PackTags` should also lead with `MultiplayerSafe` so compendium and greps match `GetEffectivePackTags` (see `tools/tag_named_fusion_materials_packtags.py`). Templates with no `PackTags` override still inherit `MultiplayerSafe` from `FusionMonsterCard` when the material is itself a fusion monster.  
   - **`FlatRaceEquipSpell`**, **`FlatSymmetricRaceFieldSpell`**, **`FlatBuffDebuffRaceFieldSpell`**, **`FlatAttributeFieldSpell`**: `_packTags` includes `MultiplayerSafe`.  
   - **Singleton field spells** (e.g. Fusion Gate, Necrovalley, A Legendary Ocean, The Sanctuary in the Sky, Mausoleum of the Emperor): each override’s `PackTags` leads with `MultiplayerSafe`.  
   - **Non-flat equips** under `Spell/Done/Equip/`: each file’s `PackTags` includes `MultiplayerSafe`.  
   - **Polymerization**, **Monster Reborn**, **Double Summon**, **Call of the Haunted**, **Lava Golem**, **Mask** equips + **Mask of Weakness** trap: `PackTags` updated.

2. **Ritual monsters and ritual spells**  
   Every `Monster/Done/Ritual/*.cs` and `Spell/Done/Ritual/*.cs` `PackTags` override leads with `MultiplayerSafe`.

3. **Retired rule mirrors**  
   - Any card whose `PackTags` included **Chance, Banish, WinCon, God, Heal, or Draw** (first sweep).  
   - **`Monster/Elemental/*.cs`** and **`Monster/Done/TrapMonster/*.cs`**.  
   - **Gravekeeper** monsters (`Gravekeeper_*` classes), **Monarch** monsters (`*_Monarch` / `Zaborg_*` etc.), **`The_First_Monarch`** trap and **`The_First_Monarch_Trap_Monster`**.  
   - All **`IDoubleTributeMaterial`** effect monsters.

4. **No catch-all pass**  
   Cards are **not** tagged merely because they override `PackTags`. After an erroneous bulk pass was reverted, eligible spell/trap/effect files keep `MultiplayerSafe` only if they match sections 1–3 above, appear on the **explicit pick list** below, or match **staple paths** wired in `tools/remediate_mp_safe_declarations.py`. Token monster bases under `Monster/Done/Token/` keep `MultiplayerSafe` on their shared `PackTags`.

5. **True normal monsters (`Monster/Done/Normal`) — adjusted-weight percentile**  
   Using the same adjusted-weight math as `YgoNormalMonsterPackTier` / `NormalMonsterCard.GetPackWeightMultiplierAdjusted`, **the top 30%** of scored true normals (by weight, descending) declare `MultiplayerSafe`, plus these legacy explicit normals always: **Gigobyte**, **Millennium_Shield**, **Skull_Servant**, **Summoned_Skull**.  
   Run `tools/patch_normal_multiplayer_safe.py` after changing tier tables or `PERCENTILE_DEFAULT` in `tools/list_high_tier_true_normals.py`.

### Declared `MultiplayerSafe` audit (approximate)

Measured as files under each subtree whose source contains `MultiplayerSafe` in `PackTags`, over all `.cs` in that subtree (post-remediation): **Effect ~57%**, **Spell ~41%**, **Trap ~45%**, **Normal ~30%** of printable true normals. Fusion monsters also inherit `MultiplayerSafe` from `FusionMonsterCard` without per-file duplication where overrides use `base.PackTags`.

## Former explicit pick list (hash-set entries, for audit)

These were listed in the deleted `YgoMultiplayerSafeExplicitPicks.Generated.cs`. **`tools/remediate_mp_safe_declarations.py`** preserves `MultiplayerSafe` on these types when stripping bulk tagging (no separate generator).

- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Airknight_Parshath`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Amazoness_Blowpiper`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Arcane_Archer_of_the_Forest`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Armed_Ninja`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Big_Shield_Gardna`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Blast_Juggler`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Bowganian`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Cannon_Soldier`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Cat_s_Ear_Tribe`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Crass_Clown`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Cyber_Jar`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.D_D_Warrior`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.D_D_Warrior_Lady`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Dark_Cat_with_White_Tail`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Dark_Elf`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Dark_Jeroid`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Dark_Magician_Girl`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Des_Koala`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Des_Volstgalph`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Dragon_Seeker`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Dream_Clown`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Drillago`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Electric_Lizard`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Enraged_Battle_Ox`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Exarion_Universe`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Exiled_Force`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Fairy_King_Truesdale`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Fear_from_the_Dark`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Freed_the_Brave_Wanderer`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Gale_Lizard`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Gora_Turtle`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Gray_Wing`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Great_Maju_Garzett`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Greenkappa`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Gyaku_Gire_Panda`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Hourglass_of_Courage`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Insect_Princess`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Insect_Soldiers_of_the_Sky`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Jinzo_7`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Karate_Man`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.King_Tiger_Wanghu`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Kotodama`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Legendary_Fiend`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Little_Winguard`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Lord_of_D`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Machine_King`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Maryokutai`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Mystic_Lamp`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Needle_Ball`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Needle_Burrower`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Nightmare_Horse`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Penguin_Soldier`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Piranha_Army`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Poison_Mummy`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Pumpking_the_King_of_Ghosts`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Queen_s_Double`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Slate_Warrior`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Spear_Cretin`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Spirit_Reaper`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Spirit_Ryu`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Stealth_Bird`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.The_Fiend_Megacyber`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.The_Hunter_with_7_Weapons`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.The_Kick_Man`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.The_Wicked_Worm_Beast`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Thunder_Dragon`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Timeater`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Trap_Master`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Troop_Dragon`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Twin_Headed_Behemoth`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Winged_Minion`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Yomi_Ship`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Zombyra_the_Dark`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Zone_Eater`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Gigobyte`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Millennium_Shield`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Skull_Servant`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Summoned_Skull`
- `YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual.Shinato_King_of_a_Higher_Plane`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos.Dark_Snake_Syndrome`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos.Stumbling`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Cold_Wave`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Cost_Down`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Diffusion_Wave_Motion`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Double_Spell`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Dragged_Down_into_the_Grave`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Hinotama`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Pyramid_Energy`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Raigeki`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Riryoku`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Sparks`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.The_Law_of_the_Normal`
- `YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Tribute_to_the_Doomed`
- `YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos.Narrow_Pass`
- `YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos.Nightmare_Wheel`
- `YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos.Spellbinding_Circle`
- `YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal.Dark_Mirror_Force`
- `YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal.Raigeki_Break`
- `YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal.Secret_Barrel`

**Note:** `Summoned_Skull` does not meet the numeric high-tier normal gate alone; `MultiplayerSafe` is declared on its `PackTags` anyway as a former explicit pick / staple.

---

## Maintenance

- **Normals:** After tier table changes, run `python tools/patch_normal_multiplayer_safe.py` (and optionally `python tools/list_high_tier_true_normals.py` to inspect the tier-ordered list).
- **Named fusion materials:** After adding or changing `FusionMonsterCard` recipes, run `python tools/tag_named_fusion_materials_packtags.py` so material monsters get declared `MultiplayerSafe` on `PackTags`.
- **Bulk corrections:** `python tools/remediate_mp_safe_declarations.py --dry-run` before applying; extend `STAPLE_RELS` / explicit-pick doc section if new staples are added.
- **New cards:** add `YgoCardPackTags.MultiplayerSafe` as the **first** bitwise flag in `PackTags` when the card should appear in multiplayer procedural pools (unless relying on Strike/Defend/power implicit rule or fusion-material effective tagging).
