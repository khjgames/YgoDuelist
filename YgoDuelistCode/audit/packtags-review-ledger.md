# PackTags review ledger

Purpose: keep a repeatable reviewed/unreviewed queue for `Cards/**` entries that declare `PackTags`, across Effect Monster, Spell, and Trap cards.

## Selection rule

- Eligible: card declares `PackTags` and is not listed under Reviewed.
- Batching: pick at least 35 cards per wave, preferring cards near active patch/service follow-ups.
- Outcome per card: `ok` (no scope issue) or `refactor` (moved behavior to card API/interface).

## Reviewed

### Wave 5 (current)

- `A_Cat_of_Ill_Omen` - `ok` (no patch/service concrete-card branch added by this card in this wave).
- `Black_Pendant` - `refactor` (graveyard trigger moved from concrete service check to card-owned hook `IYgoOnAddedToYgoGraveyardPile`).
- `Rope_of_Life` - `ok` (no misplaced patch/service card-rule branch found in this wave).

## Next unreviewed seed queue

- `Black_Pendant` batch completed this wave; keep out of queue unless behavior changes.
- Prioritize additional Spell/Trap cards with `PackTags` near active patch families (`PlayCardAction*`, `CreatureCmd*`, option-pile patches).

### Wave 6

- `Rite_of_Spirit` - `ok` (no patch/service concrete-card branch for this card in current scan).
- `Des_Counterblow` - `ok` (existing service usage is intentional card-specific orchestration; no cross-card concrete branching hotspot found).
- `Amazoness_Archers` - `ok` (no misplaced patch/service concrete-card rule found).
- `Cursed_Seal_of_the_Forbidden_Spell` - `ok` (no misplaced patch/service concrete-card rule found).
- `Secret_Barrel` - `ok` (no misplaced patch/service concrete-card rule found).
- `Call_of_the_Haunted` - `ok` (logic remains card-owned; no concrete patch branch found).
- `Embodiment_of_Apophis` - `ok` (logic remains card-owned; no concrete patch branch found).
- `The_First_Monarch` - `ok` (logic remains card-owned; no concrete patch branch found).
- `Metal_Reflect_Slime` - `ok` (logic remains card-owned; no concrete patch branch found).
- `Tribute_to_the_Doomed` - `ok` (no misplaced patch/service concrete-card rule found).

### Wave 7

- `Physical_Double` - `ok` (no misplaced patch/service concrete-card rule found).
- `Draining_Shield` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fake_Trap` - `ok` (no misplaced patch/service concrete-card rule found).
- `Curse_of_Darkness` - `refactor` (service concrete type dependency removed; card now contributes via `IYgoSpellResolvedOwnerDamageContributor`).
- `Ojama_Trio` - `ok` (no misplaced patch/service concrete-card rule found).
- `Burst_Breath` - `ok` (no misplaced patch/service concrete-card rule found).
- `Anti_Spell` - `ok` (no misplaced patch/service concrete-card rule found).
- `Enchanted_Javelin` - `ok` (no misplaced patch/service concrete-card rule found).
- `Spell_Shield_Type_8` - `ok` (no misplaced patch/service concrete-card rule found).
- `Curse_of_Aging` - `ok` (no misplaced patch/service concrete-card rule found).

### Wave 8

- `Arsenal_Robber` - `ok` (no misplaced patch/service concrete-card rule found).
- `Mask_of_Weakness` - `ok` (no misplaced patch/service concrete-card rule found).
- `Skull_Dice` - `ok` (no misplaced patch/service concrete-card rule found).
- `Reinforcements` - `ok` (no misplaced patch/service concrete-card rule found).
- `Reckless_Greed` - `ok` (no misplaced patch/service concrete-card rule found).
- `Energy_Drain` - `ok` (no misplaced patch/service concrete-card rule found).
- `Deal_of_Phantom` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dark_Spirit_of_the_Silent` - `ok` (no misplaced patch/service concrete-card rule found).
- `Curse_of_Anubis` - `ok` (no misplaced patch/service concrete-card rule found).
- `Compulsory_Evacuation_Device` - `ok` (no misplaced patch/service concrete-card rule found).
- `Castle_Walls` - `ok` (no misplaced patch/service concrete-card rule found).
- `Type_Zero_Magic_Crusher` - `ok` (no misplaced patch/service concrete-card rule found).
- `Tornado_Wall` - `ok` (no misplaced patch/service concrete-card rule found).
- `Spellbinding_Circle` - `ok` (no misplaced patch/service concrete-card rule found).
- `Soul_Resurrection` - `ok` (no misplaced patch/service concrete-card rule found).
- `Solemn_Wishes` - `ok` (no misplaced patch/service concrete-card rule found).
- `Skull_Invitation` - `ok` (intentional card-specific orchestrator service retained in this pass).
- `Nightmare_Wheel` - `ok` (no misplaced patch/service concrete-card rule found).
- `Narrow_Pass` - `refactor` (`YgoNarrowPassField` now dispatches through `IYgoMonsterCommandFieldTaxContributor` instead of concrete type checks).
- `Bottomless_Shifting_Sand` - `ok` (intentional card-specific orchestrator service retained in this pass).
- `Blind_Destruction` - `ok` (intentional card-specific orchestrator service retained in this pass).
- `Appropriate` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fairy_Box` - `ok` (existing references remain card-owned/API-based in this pass).
- `Raigeki_Break` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ominous_Fortunetelling` - `ok` (patch/service usage remains command reset orchestration).

### Wave 9

- `Tailor_of_the_Fickle` - `ok` (no misplaced patch/service concrete-card rule found).
- `Secret_Pass_to_the_Treasures` - `ok` (no misplaced patch/service concrete-card rule found).
- `Riryoku` - `ok` (payload/cleanup references remain flow orchestration; no concrete-rule violation found).
- `Emergency_Provisions` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Reliable_Guardian` - `ok` (no misplaced patch/service concrete-card rule found).
- `Rush_Recklessly` - `ok` (no misplaced patch/service concrete-card rule found).
- `Burst_Stream_of_Destruction` - `ok` (existing references remain marker/interface based; no concrete-rule violation found).
- `Diffusion_Wave_Motion` - `ok` (existing references remain marker/interface based; no concrete-rule violation found).
- `Dark_Magic_Attack` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ancient_Chant` - `ok` (existing references remain marker/interface based; no concrete-rule violation found).
- `Convulsion_of_Nature` - `ok` (existing references remain interface based; no concrete-rule violation found).
- `Fusion_Gate` - `ok` (existing references remain interface/source based; no concrete-rule violation found).
- `Insect_Armor_with_Laser_Cannon` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dragon_Nails` - `ok` (no misplaced patch/service concrete-card rule found).
- `Sword_of_Dragon_S_Soul` - `ok` (no misplaced patch/service concrete-card rule found).
- `Shield_Sword` - `ok` (no misplaced patch/service concrete-card rule found).
- `Salamandra` - `ok` (no misplaced patch/service concrete-card rule found).
- `Invigoration` - `ok` (no misplaced patch/service concrete-card rule found).
- `Cyber_Shield` - `ok` (no misplaced patch/service concrete-card rule found).
- `Elf_S_Light` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gust_Fan` - `ok` (no misplaced patch/service concrete-card rule found).
- `Archfiend_s_Oath` - `ok` (no misplaced patch/service concrete-card rule found).
- `A_Legendary_Ocean` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fairy_of_the_Spring` - `ok` (no misplaced patch/service concrete-card rule found).
- `Super_Rejuvenation` - `ok` (no misplaced patch/service concrete-card rule found).

### Wave 10

- `Cestus_of_Dagla` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dark_Piercing_Light` - `ok` (no misplaced patch/service concrete-card rule found).
- `Sparks` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_A_Forces` - `ok` (no misplaced patch/service concrete-card rule found).
- `Wasteland` - `ok` (existing Starter catalog mapping remains orchestration/data mapping).
- `Contract_with_the_Abyss` - `ok` (no misplaced patch/service concrete-card rule found).
- `Earth_Chant` - `ok` (no misplaced patch/service concrete-card rule found).
- `Zera_Ritual` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `White_Dragon_Ritual` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `War_Lion_Ritual` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Turtle_Oath` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Revival_of_Dokurorider` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Resurrection_of_Chakra` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Novox_S_Prayer` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Javelin_Beetle_Pact` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Incandescent_Ordeal` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Hamburger_Recipe` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Garma_Sword_Oath` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Fortress_Whale_S_Oath` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Curse_of_the_Masked_Beast` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Contract_with_the_Dark_Master` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Commencement_Dance` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Black_Magic_Ritual` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Black_Luster_Ritual` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Black_Illusion_Ritual` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Beastly_Mirror_Ritual` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Foolish_Burial` - `ok` (no misplaced patch/service concrete-card rule found).
- `Extra_Foolish_Burial` - `ok` (no misplaced patch/service concrete-card rule found).
- `Jam_Breeding_Machine` - `refactor` (`YgoJamBreedingMachineContinuous` now dispatches `IYgoOwnerTurnStartSpellTrapZoneEffect` instead of concrete-type checks).
- `Multiply` - `ok` (preview/reference mapping only; no concrete-rule violation found).
- `Multiplication_of_Ants` - `ok` (no misplaced patch/service concrete-card rule found).
- `Scapegoat` - `ok` (no misplaced patch/service concrete-card rule found).
- `Stray_Lambs` - `ok` (no misplaced patch/service concrete-card rule found).
- `Chaos_End` - `ok` (no misplaced patch/service concrete-card rule found).
- `Reverse_Trap` - `ok` (no misplaced patch/service concrete-card rule found).

### Wave 11

- `D_D_Designator` - `ok` (no misplaced patch/service concrete-card rule found).
- `Cold_Wave` - `ok` (no misplaced patch/service concrete-card rule found).
- `Pyramid_Energy` - `ok` (no misplaced patch/service concrete-card rule found).
- `De_Spell` - `ok` (no misplaced patch/service concrete-card rule found).
- `A_Deal_with_Dark_Ruler` - `refactor` (`YgoDealWithDarkRulerState` now dispatches owner turn-start field behavior via `IYgoOwnerTurnStartFieldMonsterEffect` instead of concrete `Berserk_Dragon` service checks).
- `Token_Thanksgiving` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dark_Hole` - `ok` (no misplaced patch/service concrete-card rule found).
- `Hinotama` - `ok` (no misplaced patch/service concrete-card rule found).
- `Raigeki` - `ok` (no misplaced patch/service concrete-card rule found).
- `Limiter_Removal` - `ok` (no misplaced patch/service concrete-card rule found).
- `Final_Destiny` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dragged_Down_into_the_Grave` - `ok` (no misplaced patch/service concrete-card rule found).
- `Double_Spell` - `ok` (no misplaced patch/service concrete-card rule found).
- `Cost_Down` - `ok` (no misplaced patch/service concrete-card rule found).
- `Graceful_Charity` - `ok` (no misplaced patch/service concrete-card rule found).
- `Terraforming` - `ok` (no misplaced patch/service concrete-card rule found).
- `Polymerization` - `ok` (no misplaced patch/service concrete-card rule found).
- `Monster_Reborn` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fusion_Sage` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Law_of_the_Normal` - `ok` (no misplaced patch/service concrete-card rule found).
- `Upstart_Goblin` - `ok` (no misplaced patch/service concrete-card rule found).
- `Spellbook_Organization` - `ok` (no misplaced patch/service concrete-card rule found).
- `Reload` - `ok` (no misplaced patch/service concrete-card rule found).
- `Reinforcement_of_the_Army` - `ok` (no misplaced patch/service concrete-card rule found).
- `Rain_of_Mercy` - `ok` (no misplaced patch/service concrete-card rule found).
- `Poison_of_the_Old_Man` - `ok` (no misplaced patch/service concrete-card rule found).
- `Nightmare_S_Steelcage` - `ok` (no misplaced patch/service concrete-card rule found).
- `Graceful_Dice` - `ok` (no misplaced patch/service concrete-card rule found).
- `Double_Summon` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dimension_Fusion` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dian_Keto_the_Cure_Master` - `ok` (no misplaced patch/service concrete-card rule found).
- `De_Fusion` - `ok` (no misplaced patch/service concrete-card rule found).
- `Card_Destruction` - `ok` (no misplaced patch/service concrete-card rule found).
- `Breath_of_Light` - `ok` (no misplaced patch/service concrete-card rule found).
- `Book_of_Taiyou` - `ok` (no misplaced patch/service concrete-card rule found).

### Wave 12

- `Book_of_Moon` - `ok` (no misplaced patch/service concrete-card rule found).
- `Yami` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Umiiruka` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Umi` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `The_Sanctuary_in_the_Sky` - `ok` (no misplaced patch/service concrete-card rule found).
- `Sogen` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Rising_Air_Current` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Necrovalley` - `ok` (starter-catalog substitutions remain orchestrator data).
- `Mystic_Plasma_Zone` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Mountain` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Molten_Destruction` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Luminous_Spark` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Gaia_Power` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Forest` - `ok` (starter-catalog/field mappings remain orchestrator data).
- `Megamorph` - `ok` (no misplaced patch/service concrete-card rule found).
- `Mask_of_the_Burdened` - `ok` (no misplaced patch/service concrete-card rule found).
- `Mask_of_Brutality` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gravity_Axe_Grarl` - `ok` (no misplaced patch/service concrete-card rule found).
- `Burning_Spear` - `ok` (no misplaced patch/service concrete-card rule found).
- `Big_Bang_Shot` - `ok` (no misplaced patch/service concrete-card rule found).
- `Axe_of_Despair` - `ok` (no misplaced patch/service concrete-card rule found).
- `Yellow_Luster_Shield` - `ok` (no misplaced patch/service concrete-card rule found).
- `Stumbling` - `ok` (existing service use remains field-orchestration and power application).
- `Dust_Barrier` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dark_Snake_Syndrome` - `ok` (no misplaced patch/service concrete-card rule found).
- `Card_Trader` - `refactor` (moved turn-start behavior to card-owned `IYgoOwnerTurnStartSpellTrapZoneEffect`; removed concrete `YgoCardTraderContinuous` service).
- `Burning_Land` - `ok` (no misplaced patch/service concrete-card rule found).
- `Banner_of_Courage` - `ok` (no misplaced patch/service concrete-card rule found).
- `Pot_Of_Greed` - `ok` (no misplaced patch/service concrete-card rule found).
- `Guardian_Slime` - `ok` (existing references remain hook/marker based).
- `Gate_Guardian` - `ok` (existing references remain data/preview mappings).
- `Slifer_the_Sky_Dragon` - `ok` (existing service uses marker interface and orchestrator behavior).
- `Obelisk_the_Tormentor` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Winged_Dragon_of_Ra` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fairy_King_Truesdale` - `ok` (no misplaced patch/service concrete-card rule found).

### Wave 13

- `Magical_Marionette` - `ok` (no misplaced patch/service concrete-card rule found).
- `Skilled_Dark_Magician` - `ok` (no misplaced patch/service concrete-card rule found).
- `Skilled_White_Magician` - `ok` (no misplaced patch/service concrete-card rule found).
- `Skilled_Red_Magician` - `ok` (no misplaced patch/service concrete-card rule found).
- `Bowganian` - `ok` (no misplaced patch/service concrete-card rule found).
- `Cyber_Jar` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Fiend_Megacyber` - `ok` (no misplaced patch/service concrete-card rule found).
- `Summoner_of_Illusions` - `ok` (no misplaced patch/service concrete-card rule found).
- `Levia_Dragon_Daedalus` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ocean_Dragon_Lord_Neo_Daedalus` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Legendary_Fisherman` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Agent_of_Wisdom_Mercury` - `ok` (no misplaced patch/service concrete-card rule found).
- `Torpedo_Fish` - `ok` (existing power-hook path remains interface based).
- `Gravekeeper_s_Assailant` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gravekeeper_s_Curse` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gravekeeper_s_Spear_Soldier` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gravekeeper_s_Cannonholder` - `ok` (no misplaced patch/service concrete-card rule found).
- `Guardian_Angel_Joan` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gora_Turtle` - `ok` (existing turn-start hook remains interface based).
- `Witch_of_the_Black_Forest` - `ok` (no misplaced patch/service concrete-card rule found).
- `Sangan` - `ok` (no misplaced patch/service concrete-card rule found).
- `Arcane_Archer_of_the_Forest` - `ok` (no misplaced patch/service concrete-card rule found).
- `Wodan_the_Resident_of_the_Forest` - `ok` (no misplaced patch/service concrete-card rule found).
- `A_Cat_of_Ill_Omen` - `ok` (already compliant in this wave re-check).
- `D_D_Scout_Plane` - `refactor` (owner end-phase return moved to card-owned `IYgoOwnerBeforeTurnEndFlushBanishedEffect`).
- `Twin_Headed_Behemoth` - `refactor` (owner end-phase graveyard revive moved to card-owned `IYgoOwnerBeforeTurnEndFlushGraveyardEffect`).
- `Manticore_of_Darkness` - `refactor` (owner end-phase graveyard summon line moved to card-owned `IYgoOwnerBeforeTurnEndFlushGraveyardEffect`).
- `Mirage_Token` - `refactor` (owner before-turn-end field cleanup moved to card-owned `IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect`).
- `Solar_Flare_Dragon` - `refactor` (owner before-turn-end field blight moved to card-owned `IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect`).
- `The_Wicked_Worm_Beast` - `refactor` (owner before-turn-end bounce moved to card-owned `IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect`).
- `Slifer_the_Sky_Dragon` - `refactor` (owner before-turn-end blight pressure follow-up moved to card-owned `IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect`).
- `Jam_Breeding_Machine` - `ok` (existing owner turn-start interface pattern remains compliant).
- `Card_Trader` - `ok` (existing owner turn-start interface pattern remains compliant).
- `Narrow_Pass` - `ok` (existing field tax interface pattern remains compliant).
- `Curse_of_Darkness` - `ok` (existing spell-resolved contributor interface pattern remains compliant).

### Wave 14

- `Windstorm_of_Etaqua` - `ok` (no misplaced patch/service concrete-card rule found).
- `Widespread_Ruin` - `ok` (no misplaced patch/service concrete-card rule found).
- `White_Hole` - `ok` (no misplaced patch/service concrete-card rule found).
- `Waboku` - `ok` (no misplaced patch/service concrete-card rule found).
- `Trap_of_Board_Eraser` - `ok` (no misplaced patch/service concrete-card rule found).
- `Trap_Hole` - `ok` (no misplaced patch/service concrete-card rule found).
- `Time_Machine` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Spell_Absorbing_Life` - `ok` (no misplaced patch/service concrete-card rule found).
- `Solemn_Judgment` - `ok` (no misplaced patch/service concrete-card rule found).
- `Solar_Ray` - `ok` (no misplaced patch/service concrete-card rule found).
- `Self_Destruct_Button` - `ok` (no misplaced patch/service concrete-card rule found).
- `Sakuretsu_Armor` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ring_of_Destruction` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ray_of_Hope` - `ok` (no misplaced patch/service concrete-card rule found).
- `Pyro_Clock_of_Destiny` - `ok` (no misplaced patch/service concrete-card rule found).
- `Pharaoh_s_Treasure` - `ok` (no misplaced patch/service concrete-card rule found).
- `Order_to_Smash` - `ok` (no misplaced patch/service concrete-card rule found).
- `Nutrient_Z` - `ok` (no misplaced patch/service concrete-card rule found).
- `Numinous_Healer` - `ok` (no misplaced patch/service concrete-card rule found).
- `Negate_Attack` - `ok` (no misplaced patch/service concrete-card rule found).
- `Mirror_Force` - `ok` (no misplaced patch/service concrete-card rule found).
- `Micro_Ray` - `ok` (no misplaced patch/service concrete-card rule found).
- `Michizure` - `ok` (no misplaced patch/service concrete-card rule found).
- `Metalmorph` - `ok` (no misplaced patch/service concrete-card rule found).
- `Magic_Cylinder` - `ok` (no misplaced patch/service concrete-card rule found).
- `Lightforce_Sword` - `ok` (no misplaced patch/service concrete-card rule found).
- `Light_of_Judgment` - `ok` (no misplaced patch/service concrete-card rule found).
- `Kunai_with_Chain` - `ok` (no misplaced patch/service concrete-card rule found).
- `Just_Desserts` - `ok` (no misplaced patch/service concrete-card rule found).
- `Judgment_of_Anubis` - `ok` (no misplaced patch/service concrete-card rule found).
- `Jar_of_Greed` - `ok` (no misplaced patch/service concrete-card rule found).
- `Hidden_Spellbook` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gryphon_Wing` - `ok` (no misplaced patch/service concrete-card rule found).
- `Graverobber` - `ok` (no misplaced patch/service concrete-card rule found).
- `Blind_Destruction` - `refactor` (moved owner turn-start dice damage behavior onto card-owned `IYgoOwnerTurnStartSpellTrapZoneEffect`; removed concrete `YgoBlindDestructionContinuous` service).

### Wave 15

- `Wild_Nature_s_Release` - `ok` (no misplaced patch/service concrete-card rule found).
- `Warrior_Elimination` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ultra_Evolution_Pill` - `ok` (no misplaced patch/service concrete-card rule found).
- `Tribute_Doll` - `ok` (no misplaced patch/service concrete-card rule found).
- `Triangle_Power` - `ok` (no misplaced patch/service concrete-card rule found).
- `Tremendous_Fire` - `ok` (no misplaced patch/service concrete-card rule found).
- `Thunder_Crash` - `ok` (no misplaced patch/service concrete-card rule found).
- `Thousand_Knives` - `ok` (no misplaced patch/service concrete-card rule found).
- `Thousand_Energy` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Warrior_Returning_Alive` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Shallow_Grave` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Mask_of_Remnants` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Inexperienced_Spy` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Flute_of_Summoning_Dragon` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Cheerful_Coffin` - `ok` (no misplaced patch/service concrete-card rule found).
- `Swords_of_Revealing_Light` - `ok` (no misplaced patch/service concrete-card rule found).
- `Stamping_Destruction` - `ok` (no misplaced patch/service concrete-card rule found).
- `Spiritualism` - `ok` (no misplaced patch/service concrete-card rule found).
- `Spell_Reproduction` - `ok` (no misplaced patch/service concrete-card rule found).
- `Soul_of_the_Pure` - `ok` (no misplaced patch/service concrete-card rule found).
- `Soul_Reversal` - `ok` (no misplaced patch/service concrete-card rule found).
- `Soul_Release` - `ok` (no misplaced patch/service concrete-card rule found).
- `Soul_Exchange` - `ok` (no misplaced patch/service concrete-card rule found).
- `Smashing_Ground` - `ok` (no misplaced patch/service concrete-card rule found).
- `Share_the_Pain` - `ok` (no misplaced patch/service concrete-card rule found).
- `Sebek_S_Blessing` - `ok` (no misplaced patch/service concrete-card rule found).
- `Seal_of_the_Ancients` - `ok` (no misplaced patch/service concrete-card rule found).
- `Salvage` - `ok` (no misplaced patch/service concrete-card rule found).
- `Return_of_the_Doomed` - `ok` (no misplaced patch/service concrete-card rule found).
- `Restructer_Revolution` - `ok` (no misplaced patch/service concrete-card rule found).
- `Mystical_Space_Typhoon` - `ok` (no misplaced patch/service concrete-card rule found).
- `Heavy_Storm` - `ok` (no misplaced patch/service concrete-card rule found).
- `Enemy_Controller` - `ok` (no misplaced patch/service concrete-card rule found).
- `Premature_Burial` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dark_Room_of_Nightmare` - `ok` (no misplaced patch/service concrete-card rule found).

- Infrastructure application in this wave (same timing-bucket normalization): `Slifer_the_Sky_Dragon`, `Gora_Turtle`, and `Berserk_Dragon` now resolve via `IYgoOwnerTurnStartFieldMonsterEffect` + `YgoOwnerTurnStartFieldMonsterHooks` dispatch.

### Wave 16

- `Magical_Arm_Shield` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gorgon_S_Eye` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gift_of_The_Mystical_Elf` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fiend_s_Hand_Mirror` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fiend_Comedian` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dice_Re_Roll` - `ok` (no misplaced patch/service concrete-card rule found).
- `D_Tribe` - `ok` (no misplaced patch/service concrete-card rule found).
- `Crush_Card_Virus` - `ok` (no misplaced patch/service concrete-card rule found).
- `Chain_Disappearance` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ceasefire` - `ok` (no misplaced patch/service concrete-card rule found).
- `Call_of_the_Grave` - `ok` (no misplaced patch/service concrete-card rule found).
- `Blasting_the_Ruins` - `ok` (no misplaced patch/service concrete-card rule found).
- `Blast_with_Chain` - `ok` (no misplaced patch/service concrete-card rule found).
- `Blast_Held_by_a_Tribute` - `ok` (no misplaced patch/service concrete-card rule found).
- `Big_Burn` - `ok` (no misplaced patch/service concrete-card rule found).
- `Beckoning_Light` - `ok` (no misplaced patch/service concrete-card rule found).
- `Barrel_Behind_the_Door` - `ok` (no misplaced patch/service concrete-card rule found).
- `Backup_Soldier` - `ok` (no misplaced patch/service concrete-card rule found).
- `Attack_and_Receive` - `ok` (no misplaced patch/service concrete-card rule found).
- `Anti_Raigeki` - `ok` (no misplaced patch/service concrete-card rule found).
- `Adhesion_Trap_Hole` - `ok` (no misplaced patch/service concrete-card rule found).
- `Acid_Trap_Hole` - `ok` (no misplaced patch/service concrete-card rule found).
- `Statue_of_the_Wicked` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dark_Mirror_Force` - `ok` (no misplaced patch/service concrete-card rule found).
- `Bottomless_Trap_Hole` - `ok` (no misplaced patch/service concrete-card rule found).
- `Red_Medicine` - `ok` (no misplaced patch/service concrete-card rule found).
- `Reasoning` - `ok` (no misplaced patch/service concrete-card rule found).
- `Raimei` - `ok` (no misplaced patch/service concrete-card rule found).
- `Puppet_Ritual` - `ok` (no misplaced patch/service concrete-card rule found).
- `Primal_Seed` - `ok` (no misplaced patch/service concrete-card rule found).
- `Painful_Choice` - `ok` (no misplaced patch/service concrete-card rule found).
- `Order_to_Charge` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ookazi` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ojama_Delta_Hurricane` - `ok` (no misplaced patch/service concrete-card rule found).
- `Offerings_to_the_Doomed` - `ok` (no misplaced patch/service concrete-card rule found).

- Infrastructure application in this wave (same timing-bucket normalization): `Bottomless_Shifting_Sand` now resolves via `IYgoOwnerBeforeTurnEndFlushSpellTrapZoneEffect` + `YgoOwnerBeforeTurnEndFlushHooks` spell/trap-zone dispatch.

### Wave 17

- `Legendary_Fiend` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fear_from_the_Dark` - `ok` (no misplaced patch/service concrete-card rule found).
- `Timeater` - `ok` (no misplaced patch/service concrete-card rule found).
- `D_D_Warrior_Lady` - `ok` (no misplaced patch/service concrete-card rule found).
- `D_D_Warrior` - `ok` (no misplaced patch/service concrete-card rule found).
- `Chaos_Command_Magician` - `ok` (no misplaced patch/service concrete-card rule found).
- `Dancing_Fairy` - `ok` (no misplaced patch/service concrete-card rule found).
- `Minar` - `ok` (no misplaced patch/service concrete-card rule found).
- `Command_Knight` - `ok` (no misplaced patch/service concrete-card rule found).
- `Ultimate_Obedient_Fiend` - `ok` (no misplaced patch/service concrete-card rule found).
- `Winged_Minion` - `ok` (no misplaced patch/service concrete-card rule found).
- `Anti_Aircraft_Flower` - `ok` (no misplaced patch/service concrete-card rule found).
- `Karate_Man` - `ok` (no misplaced patch/service concrete-card rule found).
- `Masked_Sorcerer` - `ok` (no misplaced patch/service concrete-card rule found).
- `Electric_Lizard` - `ok` (no misplaced patch/service concrete-card rule found).
- `Yomi_Ship` - `ok` (no misplaced patch/service concrete-card rule found).
- `Zone_Eater` - `ok` (no misplaced patch/service concrete-card rule found).
- `Giant_Rat` - `ok` (no misplaced patch/service concrete-card rule found).
- `Sonic_Bird` - `ok` (no misplaced patch/service concrete-card rule found).
- `Senju_of_the_Thousand_Hands` - `ok` (no misplaced patch/service concrete-card rule found).
- `Manju_of_the_Ten_Thousand_Hands` - `ok` (no misplaced patch/service concrete-card rule found).
- `Goddess_of_Whim` - `ok` (no misplaced patch/service concrete-card rule found).
- `Helping_Robo_for_Combat` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gravekeeper_s_Chief` - `ok` (no misplaced patch/service concrete-card rule found).
- `Woodland_Sprite` - `ok` (no misplaced patch/service concrete-card rule found).
- `Hysteric_Fairy` - `ok` (no misplaced patch/service concrete-card rule found).
- `Granadora` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gray_Wing` - `ok` (no misplaced patch/service concrete-card rule found).
- `Spirit_Ryu` - `ok` (no misplaced patch/service concrete-card rule found).
- `Tyrant_Dragon` - `ok` (no misplaced patch/service concrete-card rule found).
- `Gilford_the_Lightning` - `ok` (no misplaced patch/service concrete-card rule found).
- `Maju_Garzett` - `ok` (no misplaced patch/service concrete-card rule found).
- `The_Kick_Man` - `ok` (no misplaced patch/service concrete-card rule found).
- `Chaosrider_Gustaph` - `ok` (no misplaced patch/service concrete-card rule found).
- `Fire_Sorcerer` - `ok` (no misplaced patch/service concrete-card rule found).

- Infrastructure application in this wave: `The_Sanctuary_in_the_Sky` mercury draw + annual gate moved off `GraveyardRelic` onto `IYgoOwnerTurnStartSpellTrapZoneEffect` with `YgoOwnerTurnStartSpellTrapDispatchPhase.BeforeOwnerFieldPetHooks`; `YgoJamBreedingMachineContinuous.TryResolvePlayerTurnStartForPhase` + default interface phase keep post-Sealmaster timing for existing zone hooks (`refactor` for `The_Sanctuary_in_the_Sky` relative to Wave 12 `ok` snapshot).
