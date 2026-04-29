# YgoDuelist — implemented card inventory (generated)

**Do not edit by hand.** Regenerate with:

```text
python tools/card_inventory_scan.py
python tools/generate_card_inventory_md.py
```

## Summary counts

- **implemented**: 449
- **stubs**: 534
- **fusionExtra**: 0
- **ritualExtra**: 0
- **totalSealedClassesExcludingNormalMonsters**: 983

**Fusion monsters with C# logic beyond materials:** 0 (see `fusionWithExtraLogic` in `card_inventory_data.json`).

**Ritual monsters with C# logic beyond empty ritual frame:** 0 (see `ritualWithExtraLogic`).

## ContinuousSpell (9)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `Archfiend_s_Oath` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Archfiend_s_Oath.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Burning_Land` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Burning_Land.cs` | Destroy any active field spells, all enemies take 5 damage at the end of your turn (same as poison timing) |
| `Convulsion_of_Nature` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Convulsion_of_Nature.cs` | Allways reveal the top card of your draw pile |
| `Dark_Snake_Syndrome` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Dark_Snake_Syndrome.cs` | 3 (2) cost, damages target enemy for 1, doubling each turn, capped at 64. |
| `Dust_Barrier` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Dust_Barrier.cs` | 1 (0) cost, Normal monsters are unaffected by most debuffs (strength, dex, weakened, frail ), This card only lasts 2 turns. |
| `Stumbling` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Stumbling.cs` | 1 cost, every turn: all enemies lose 1 temp strength, newly summoned monsters can only defend. |
| `Talisman_of_Trap_Sealing` | `YgoDuelistCode/Cards/Spell/Done/Continuos/Talisman_of_Trap_Sealing.cs` | 0 cost, uncommon, Once per turn you can exhaust two status/curse cards in your hand. |
| `The_A_Forces` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/The_A_Forces.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Yellow_Luster_Shield` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Yellow_Luster_Shield.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## EffectMonster (45)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `Amazoness_Blowpiper` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Amazoness_Blowpiper.cs` | can apply weak |
| `Amazoness_Swords_Woman` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Amazoness_Swords_Woman.cs` | gives you 3 thorns |
| `Amazoness_Tiger` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Amazoness_Tiger.cs` | 4 atk per amazoness monster |
| `Ameba` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Ameba.cs` | 0 cost 3/3, 3 cost self destruct deal 20 damage |
| `Anti_Aircraft_Flower` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Anti_Aircraft_Flower.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Arcane_Archer_of_the_Forest` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Arcane_Archer_of_the_Forest.cs` | can tribute 1 earth monster, inflict 1 weaken, 3 vulnerable. |
| `Bladefly` | `YgoDuelistCode/Cards/Monster/Elemental/Bladefly.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Blast_Juggler` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Blast_Juggler.cs` | 0 cost, sacrifice this monster, up to two unique targets take 10 damage |
| `Bowganian` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Bowganian.cs` | once per turn,  0 cost 6 damage. |
| `Burning_Algae` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Burning_Algae.cs` | 0 cost 5 attack, 2 cost block 15, heals all enemies 10 upon destruction. |
| `Cat_s_Ear_Tribe` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Cat_s_Ear_Tribe.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Chaos_Command_Magician` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Chaos_Command_Magician.cs` | Gain 2 artifact |
| `Crass_Clown` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Crass_Clown.cs` | When changed from defense to attack, target one enemy, apply 1 weak and deal 6 damage. |
| `Cure_Mermaid` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Cure_Mermaid.cs` | 2 cost 15 atk, 1 cost 8 def, Forced die for you, once per turn, takes 1 (0) damage and heals you 1 hp. |
| `Cyber_Jar` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Cyber_Jar.cs` | 3 cost, nuke your monsters, reveal 5 (6) , special summon any level 4 or lower monsters, gain free casts of their command attack / defends this turn. |
| `D_D_Crazy_Beast` | `YgoDuelistCode/Cards/Monster/Todo/Effect/D_D_Crazy_Beast.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `D_D_Warrior_Lady` | `YgoDuelistCode/Cards/Monster/Todo/Effect/D_D_Warrior_Lady.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dancing_Fairy` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Dancing_Fairy.cs` | 2 energy, 10 block, heal 1 hp. |
| `Dark_Cat_with_White_Tail` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Dark_Cat_with_White_Tail.cs` | On flip, return 1 monster to hand and inflict all enemies with 1 vulnerable and weaken |
| `Dark_Jeroid` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Dark_Jeroid.cs` | When summoned apply 1 weak to target enemy |
| `Dark_Zebra` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Dark_Zebra.cs` | After being summoned, cannot attack when its the only monster you control. |
| `Des_Kangaroo` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Des_Kangaroo.cs` | defense grants 2 temporary thorns |
| `Dream_Clown` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Dream_Clown.cs` | When changed from attack to defense, target one enemy, apply 1 vulnerable and deal 6 damage. |
| `Enraged_Muka_Muka` | `YgoDuelistCode/Cards/Monster/Enraged_Muka_Muka.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Exiled_Force` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Exiled_Force.cs` | You can tribute this card to deal 10 damage. |
| `Fairy_Guardian` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Fairy_Guardian.cs` | Tribute this card, return a spell card from your graveyard to the bottom of your draw pile. |
| `Hoshiningen` | `YgoDuelistCode/Cards/Monster/Elemental/Hoshiningen.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Jinzo_7` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Jinzo_7.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Little_Chimera` | `YgoDuelistCode/Cards/Monster/Elemental/Little_Chimera.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Lord_of_D` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Lord_of_D.cs` | Dragon monsters gain 200 atk & def. |
| `Milus_Radiant` | `YgoDuelistCode/Cards/Monster/Elemental/Milus_Radiant.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Muka_Muka` | `YgoDuelistCode/Cards/Monster/Muka_Muka.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Possessed_Dark_Soul` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Possessed_Dark_Soul.cs` | Tribute this card, inflict 1 weak on all enemies |
| `Star_Boy` | `YgoDuelistCode/Cards/Monster/Elemental/Star_Boy.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sword_Hunter` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Sword_Hunter.cs` | When this card executes an enemy permanently increase its attack by 3 (4) |
| `Tainted_Wisdom` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Tainted_Wisdom.cs` | re shuffle your discard pile into your draw pile |
| `Terrorking_Archfiend` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Terrorking_Archfiend.cs` | When this card executes an enemy permanently increase its attack by 3 (4) |
| `The_Legendary_Fisherman` | `YgoDuelistCode/Cards/Monster/Todo/Effect/The_Legendary_Fisherman.cs` | While Umi is on the field, this card costs 1 [E] less. |
| `Thunder_Dragon` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Thunder_Dragon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Toon_Dark_Magician_Girl` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Toon_Dark_Magician_Girl.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Toon_Mermaid` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Toon_Mermaid.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Toon_Summoned_Skull` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Toon_Summoned_Skull.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Witchs_Apprentice` | `YgoDuelistCode/Cards/Monster/Elemental/Witchs_Apprentice.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Zombyra_the_Dark` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Zombyra_the_Dark.cs` | 1 cost, if this card executes a monster it permanently loses 2(1) attack. |
| `Zone_Eater` | `YgoDuelistCode/Cards/Monster/Todo/Effect/Zone_Eater.cs` | Inflicts "Zone Eater Power" -> Zone Eater Power - Individual powers -> After 5 turns enemy takes 20 damage |

## EquipSpell (15)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `Axe_of_Despair` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Axe_of_Despair.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Burning_Beast` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Burning_Beast.cs` | Equip effect (monster attacks inflict 1 weak and vulnerable) |
| `Burning_Spear` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Burning_Spear.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Cestus_of_Dagla` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Cestus_of_Dagla.cs` | 1 cost, restore 1 health when equipped monster attacks |
| `Cyber_Shield` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Cyber_Shield.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dragon_Nails` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Dragon_Nails.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Elf_S_Light` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Elf_S_Light.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Gust_Fan` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Gust_Fan.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Insect_Armor_with_Laser_Cannon` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Insect_Armor_with_Laser_Cannon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Invigoration` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Invigoration.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Salamandra` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Salamandra.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Shine_Palace` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Shine_Palace.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Steel_Shell` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Steel_Shell.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sword_of_Dark_Destruction` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Sword_of_Dark_Destruction.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sword_of_Dragon_S_Soul` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Sword_of_Dragon_S_Soul.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## FieldSpell (15)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `A_Legendary_Ocean` | `YgoDuelistCode/Cards/Spell/Todo/Field/A_Legendary_Ocean.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Forest` | `YgoDuelistCode/Cards/Spell/Todo/Field/Forest.cs` | can tribute 1 earth monster, inflict 1 weaken, 3 vulnerable. |
| `Gaia_Power` | `YgoDuelistCode/Cards/Spell/Todo/Field/Gaia_Power.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Luminous_Spark` | `YgoDuelistCode/Cards/Spell/Todo/Field/Luminous_Spark.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Molten_Destruction` | `YgoDuelistCode/Cards/Spell/Todo/Field/Molten_Destruction.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mountain` | `YgoDuelistCode/Cards/Spell/Todo/Field/Mountain.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mystic_Plasma_Zone` | `YgoDuelistCode/Cards/Spell/Todo/Field/Mystic_Plasma_Zone.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Necrovalley` | `YgoDuelistCode/Cards/Spell/Todo/Field/Necrovalley.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rising_Air_Current` | `YgoDuelistCode/Cards/Spell/Todo/Field/Rising_Air_Current.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sogen` | `YgoDuelistCode/Cards/Spell/Todo/Field/Sogen.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Sanctuary_in_the_Sky` | `YgoDuelistCode/Cards/Spell/Todo/Field/The_Sanctuary_in_the_Sky.cs` | You take half damage from attacks sacrificially blocked by fairy type monsters. |
| `Umi` | `YgoDuelistCode/Cards/Spell/Todo/Field/Umi.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Umiiruka` | `YgoDuelistCode/Cards/Spell/Todo/Field/Umiiruka.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Wasteland` | `YgoDuelistCode/Cards/Spell/Todo/Field/Wasteland.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Yami` | `YgoDuelistCode/Cards/Spell/Todo/Field/Yami.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## FusionSpell (1)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `Polymerization` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Polymerization.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## Other (24)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `Activate_Effect` | `YgoDuelistCode/Cards/Command/Activate_Effect.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Anti_Spell_Fragrance` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Anti_Spell_Fragrance.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Appropriate` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Appropriate.cs` | once per turn, when you draw a status you can draw a card. |
| `Aqua_Chorus` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Aqua_Chorus.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Bad_Reaction_to_Simochi` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Bad_Reaction_to_Simochi.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Blind_Destruction` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Blind_Destruction.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Bottomless_Shifting_Sand` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Bottomless_Shifting_Sand.cs` | destroyed if you end a turn with less than 4 cards in hand, the enemy with the highest attack intent each turn takes damage equal to its attack (max 30) |
| `Command_Attack` | `YgoDuelistCode/Cards/Command/Command_Attack.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Command_Change_Battle_Position` | `YgoDuelistCode/Cards/Command/Command_Change_Battle_Position.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Command_Defend` | `YgoDuelistCode/Cards/Command/Command_Defend.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Curse_of_Darkness` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Curse_of_Darkness.cs` | 1 cost, every time you play a spell a random enemy takes 6 damage. |
| `Des_Counterblow` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Des_Counterblow.cs` | 1 cost gain 4 thorns |
| `Exit_Monster_Options` | `YgoDuelistCode/Cards/Command/Exit_Monster_Options.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fairy_Box` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Fairy_Box.cs` | If you call it right all enemies gain 1 weaken, once per turn, take 5 damage (blockable) or destroy this card. |
| `Goblin_Fan` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Goblin_Fan.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Magical_Thorn` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Magical_Thorn.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Royal_Decree` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Royal_Decree.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Shadow_Spell` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Shadow_Spell.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spellbinding_Circle` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Spellbinding_Circle.cs` | 1 (0) cost, every turn: enemy loses 1 temp strength & gains 1 spell bound. (Spellbound effect: enemy takes 1 damage per stack, same time as poison) |
| `Talisman_of_Spell_Sealing` | `YgoDuelistCode/Cards/Trap/Done/Continuos/Talisman_of_Spell_Sealing.cs` | 0 cost, uncommon, Once per turn gain 2 temporary artifact. |
| `Toggle_Die_For_You` | `YgoDuelistCode/Cards/Command/Toggle_Die_For_You.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Tornado_Wall` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Tornado_Wall.cs` | Every turn. All enemies lose 1 temp strength. |
| `Type_Zero_Magic_Crusher` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Type_Zero_Magic_Crusher.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ultimate_Offering` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Ultimate_Offering.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## RitualSpell (22)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `Beastly_Mirror_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Beastly_Mirror_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Black_Illusion_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Black_Illusion_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Black_Luster_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Black_Luster_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Black_Magic_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Black_Magic_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Commencement_Dance` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Commencement_Dance.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Contract_with_the_Abyss` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Contract_with_the_Abyss.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Contract_with_the_Dark_Master` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Contract_with_the_Dark_Master.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Curse_of_the_Masked_Beast` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Curse_of_the_Masked_Beast.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Earth_Chant` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Earth_Chant.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fortress_Whale_S_Oath` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Fortress_Whale_S_Oath.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Garma_Sword_Oath` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Garma_Sword_Oath.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Hamburger_Recipe` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Hamburger_Recipe.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Incandescent_Ordeal` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Incandescent_Ordeal.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Javelin_Beetle_Pact` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Javelin_Beetle_Pact.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Novox_S_Prayer` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Novox_S_Prayer.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Resurrection_of_Chakra` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Resurrection_of_Chakra.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Revival_of_Dokurorider` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Revival_of_Dokurorider.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Shinato_S_Ark` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Shinato_s_Ark.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Turtle_Oath` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Turtle_Oath.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `War_Lion_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/War_Lion_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `White_Dragon_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/White_Dragon_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Zera_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Ritual/Zera_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## Spell (200)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `A_Deal_with_Dark_Ruler` | `YgoDuelistCode/Cards/Spell/Todo/Normal/A_Deal_with_Dark_Ruler.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `A_Wingbeat_of_Giant_Dragon` | `YgoDuelistCode/Cards/Spell/Todo/Normal/A_Wingbeat_of_Giant_Dragon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Autonomous_Action_Unit` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Autonomous_Action_Unit.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Banner_of_Courage` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Banner_of_Courage.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Big_Bang_Shot` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Big_Bang_Shot.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Black_Pendant` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Black_Pendant.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Blue_Medicine` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Blue_Medicine.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Book_of_Life` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Book_of_Life.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Book_of_Moon` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Book_of_Moon.cs` | strength - 4 |
| `Book_of_Taiyou` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Book_of_Taiyou.cs` | break all block & remove all artifact |
| `Brain_Control` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Brain_Control.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Breath_of_Light` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Breath_of_Light.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Burst_Stream_of_Destruction` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Burst_Stream_of_Destruction.cs` | 0 cost, target blue-eyes, all enemies take damage equal to targeted blue-eyes attack. |
| `Buster_Rancher` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Buster_Rancher.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Butterfly_Dagger_Elma` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Butterfly_Dagger_Elma.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Call_of_the_Mummy` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Call_of_the_Mummy.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Card_7_Completed` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Card_7_Completed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Card_Destruction` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Card_Destruction.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Card_Shuffle` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Card_Shuffle.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Card_Trader` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Card_Trader.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Card_of_Safe_Return` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Card_of_Safe_Return.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Change_of_Heart` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Change_of_Heart.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Chaos_End` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Chaos_End.cs` | uncommon, 1 cost, 5 damage per card in Banished |
| `Chosen_One` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Chosen_One.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Cold_Wave` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Cold_Wave.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Contract_with_Exodia` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Contract_with_Exodia.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Cost_Down` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Cost_Down.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `D_D_Designator` | `YgoDuelistCode/Cards/Spell/Todo/Normal/D_D_Designator.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dark_Hole` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dark_Hole.cs` | 1 energy, deal 20 damage to all enemies and all monsters you control. |
| `Dark_Magic_Attack` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dark_Magic_Attack.cs` | 0 cost, Inflict 3 vulnerable & 3 weaken on all enemies. |
| `Dark_Magic_Curtain` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dark_Magic_Curtain.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dark_Piercing_Light` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dark_Piercing_Light.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dark_Room_of_Nightmare` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Dark_Room_of_Nightmare.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `De_Fusion` | `YgoDuelistCode/Cards/Spell/Todo/Normal/De_Fusion.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `De_Spell` | `YgoDuelistCode/Cards/Spell/Todo/Normal/De_Spell.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dedication_through_Light_and_Darkness` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dedication_through_Light_and_Darkness.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dian_Keto_the_Cure_Master` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dian_Keto_the_Cure_Master.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Different_Dimension_Capsule` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Different_Dimension_Capsule.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Diffusion_Wave_Motion` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Diffusion_Wave_Motion.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dimension_Fusion` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dimension_Fusion.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Double_Spell` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Double_Spell.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Double_Summon` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Double_Summon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dragged_Down_into_the_Grave` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dragged_Down_into_the_Grave.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dragon_s_Gunfire` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Dragon_s_Gunfire.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dragonic_Attack` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Dragonic_Attack.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Elegant_Egotist` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Elegant_Egotist.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Emblem_of_Dragon_Destroyer` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Emblem_of_Dragon_Destroyer.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Emergency_Provisions` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Emergency_Provisions.cs` | Destroy any number of spell / trap cards you control, heal 1 hp per. |
| `Enchanting_Fitting_Room` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Enchanting_Fitting_Room.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Enemy_Controller` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Enemy_Controller.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Eradicating_Aerosol` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Eradicating_Aerosol.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Exchange` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Exchange.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Exile_of_the_Wicked` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Exile_of_the_Wicked.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Extra_Foolish_Burial` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Extra_Foolish_Burial.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fairy_of_the_Spring` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Fairy_of_the_Spring.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Final_Countdown` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Final_Countdown.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Final_Destiny` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Final_Destiny.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Final_Flame` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Final_Flame.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Foolish_Burial` | `YgoDuelistCode/Cards/Spell/Foolish_Burial.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fusion_Gate` | `YgoDuelistCode/Cards/Spell/Todo/Field/Fusion_Gate.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fusion_Sage` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Fusion_Sage.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fusion_Sword_Murasame_Blade` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Fusion_Sword_Murasame_Blade.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Giant_Trunade` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Giant_Trunade.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Goblin_Thief` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Goblin_Thief.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Goblin_s_Secret_Remedy` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Goblin_s_Secret_Remedy.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Graceful_Charity` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Graceful_Charity.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Graceful_Dice` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Graceful_Dice.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Gravedigger_Ghoul` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Gravedigger_Ghoul.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Gravity_Axe_Grarl` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Gravity_Axe_Grarl.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Gryphon_s_Feather_Duster` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Gryphon_s_Feather_Duster.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Harpie_S_Feather_Duster` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Harpie_S_Feather_Duster.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Heart_of_the_Underdog` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Heart_of_the_Underdog.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Heavy_Storm` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Heavy_Storm.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Hinotama` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Hinotama.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Horn_of_Light` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Horn_of_Light.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Horn_of_the_Unicorn` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Horn_of_the_Unicorn.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Insect_Imitation` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Insect_Imitation.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Inspection` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Inspection.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Jam_Breeding_Machine` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Jam_Breeding_Machine.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Last_Day_of_Witch` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Last_Day_of_Witch.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Last_Will` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Last_Will.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Lightning_Blade` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Lightning_Blade.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Limiter_Removal` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Limiter_Removal.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mage_Power` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Mage_Power.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Magical_Labyrinth` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Magical_Labyrinth.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Makiu_the_Magical_Mist` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Makiu_the_Magical_Mist.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Malevolent_Nuzzler` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Malevolent_Nuzzler.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `March_of_the_Monarchs` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/March_of_the_Monarchs.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mask_of_Brutality` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Mask_of_Brutality.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mask_of_the_Accursed` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Mask_of_the_Accursed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mass_Driver` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Mass_Driver.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mega_Ton_Magical_Cannon` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Mega_Ton_Magical_Cannon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Megamorph` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Megamorph.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mesmeric_Control` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Mesmeric_Control.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Metamorphosis` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Metamorphosis.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mimicat` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Mimicat.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Miracle_Dig` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Miracle_Dig.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mirage_of_Nightmare` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Mirage_of_Nightmare.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Monster_Gate` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Monster_Gate.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Monster_Reborn` | `YgoDuelistCode/Cards/Spell/Monster_Reborn.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Monster_Recovery` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Monster_Recovery.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mooyan_Curry` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Mooyan_Curry.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Morale_Boost` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Morale_Boost.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Multiplication_of_Ants` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Multiplication_of_Ants.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Multiply` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Multiply.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mystic_Box` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Mystic_Box.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mystical_Space_Typhoon` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Mystical_Space_Typhoon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mystik_Wok` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Mystik_Wok.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Nightmare_S_Steelcage` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Nightmare_S_Steelcage.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Offerings_to_the_Doomed` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Offerings_to_the_Doomed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ojama_Delta_Hurricane` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Ojama_Delta_Hurricane.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ookazi` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Ookazi.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Opti_Camouflage_Armor` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Opti_Camouflage_Armor.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Order_to_Charge` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Order_to_Charge.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Painful_Choice` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Painful_Choice.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Pandemonium` | `YgoDuelistCode/Cards/Spell/Todo/Field/Pandemonium.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Paralyzing_Potion` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Paralyzing_Potion.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Poison_of_the_Old_Man` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Poison_of_the_Old_Man.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Pot_Of_Greed` | `YgoDuelistCode/Cards/Spell/Pot_Of_Greed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Precious_Cards_from_Beyond` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Precious_Cards_from_Beyond.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Premature_Burial` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Premature_Burial.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Primal_Seed` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Primal_Seed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Puppet_Ritual` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Puppet_Ritual.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Pyramid_Energy` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Pyramid_Energy.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Raigeki` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Raigeki.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Raimei` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Raimei.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rain_of_Mercy` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Rain_of_Mercy.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Reasoning` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Reasoning.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Recycle` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Recycle.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Red_Medicine` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Red_Medicine.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Reinforcement_of_the_Army` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Reinforcement_of_the_Army.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Reload` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Reload.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Restructer_Revolution` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Restructer_Revolution.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Return_of_the_Doomed` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Return_of_the_Doomed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Return_of_the_Monarchs` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Return_of_the_Monarchs.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Riryoku` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Riryoku.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rod_of_Silence_Kay_est` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Rod_of_Silence_Kay_est.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rod_of_the_Mind_s_Eye` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Rod_of_the_Mind_s_Eye.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rush_Recklessly` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Rush_Recklessly.cs` | 1 (0) cost, draw a card. |
| `Salvage` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Salvage.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Scapegoat` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Scapegoat.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Scroll_of_Bewitchment` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Scroll_of_Bewitchment.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Seal_of_the_Ancients` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Seal_of_the_Ancients.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sebek_S_Blessing` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Sebek_S_Blessing.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Second_Coin_Toss` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Second_Coin_Toss.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Secret_Pass_to_the_Treasures` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Secret_Pass_to_the_Treasures.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Senri_Eye` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Senri_Eye.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Share_the_Pain` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Share_the_Pain.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Shield_Sword` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Shield_Sword.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Shooting_Star_Bow_Ceal` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Shooting_Star_Bow_Ceal.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Smashing_Ground` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Smashing_Ground.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Soul_Absorption` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Soul_Absorption.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Soul_Exchange` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Soul_Exchange.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Soul_Release` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Soul_Release.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Soul_Reversal` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Soul_Reversal.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Soul_of_the_Pure` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Soul_of_the_Pure.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sparks` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Sparks.cs` | 0 cost, deal 2 (5), draw a card. |
| `Spell_Economics` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Spell_Economics.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spell_Reproduction` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Spell_Reproduction.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spellbook_Organization` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Spellbook_Organization.cs` | 0 cost, upgrades to add draw 1 to its effect |
| `Spirit_Message_A` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Spirit_Message_A.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spirit_Message_I` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Spirit_Message_I.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spirit_Message_L` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Spirit_Message_L.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spirit_Message_N` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Spirit_Message_N.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spiritualism` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Spiritualism.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Stamping_Destruction` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Stamping_Destruction.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Stray_Lambs` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Stray_Lambs.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Super_Rejuvenation` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Super_Rejuvenation.cs` | 0 cost, Next turn draw (+1) additional cards equal to the number of dragon monsters destroyed this turn. |
| `Sword_of_Deep_Seated` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Sword_of_Deep_Seated.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sword_of_the_Soul_Eater` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Sword_of_the_Soul_Eater.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Swords_of_Revealing_Light` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Swords_of_Revealing_Light.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Tailor_of_the_Fickle` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Tailor_of_the_Fickle.cs` | 0 cost, upgrades to add draw 1 to its effect |
| `Terraforming` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Terraforming.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Cheerful_Coffin` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Cheerful_Coffin.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Flute_of_Summoning_Dragon` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Flute_of_Summoning_Dragon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Inexperienced_Spy` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Inexperienced_Spy.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Law_of_the_Normal` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Law_of_the_Normal.cs` | Deal Damage To All Enemies equal to their Combined Attack 4 (5) times. Destroy field & hand except those monsters. |
| `The_Mask_of_Remnants` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Mask_of_Remnants.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Reliable_Guardian` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Reliable_Guardian.cs` | 1 (0) cost, draw a card. |
| `The_Second_Sarcophagus` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/The_Second_Sarcophagus.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Shallow_Grave` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Shallow_Grave.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Third_Sarcophagus` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/The_Third_Sarcophagus.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Warrior_Returning_Alive` | `YgoDuelistCode/Cards/Spell/Todo/Normal/The_Warrior_Returning_Alive.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Thousand_Energy` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Thousand_Energy.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Thousand_Knives` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Thousand_Knives.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Thunder_Crash` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Thunder_Crash.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Token_Thanksgiving` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Token_Thanksgiving.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Toon_World` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Toon_World.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Tremendous_Fire` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Tremendous_Fire.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Triangle_Power` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Triangle_Power.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Tribute_Doll` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Tribute_Doll.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Tribute_to_the_Doomed` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Tribute_to_the_Doomed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Twin_Swords_of_Flashing_Light_Tryce` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Twin_Swords_of_Flashing_Light_Tryce.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ultra_Evolution_Pill` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Ultra_Evolution_Pill.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `United_We_Stand` | `YgoDuelistCode/Cards/Spell/Todo/Equip/United_We_Stand.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Upstart_Goblin` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Upstart_Goblin.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Warrior_Elimination` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Warrior_Elimination.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Wave_Motion_Cannon` | `YgoDuelistCode/Cards/Spell/Todo/Continuos/Wave_Motion_Cannon.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Wicked_Breaking_Flamberge_Baou` | `YgoDuelistCode/Cards/Spell/Todo/Equip/Wicked_Breaking_Flamberge_Baou.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Wild_Nature_s_Release` | `YgoDuelistCode/Cards/Spell/Todo/Normal/Wild_Nature_s_Release.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## Trap (118)

| Class | File | Revised effect (Cards_Revised.md) |
|-------|------|-----------------------------------|
| `Acid_Trap_Hole` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Acid_Trap_Hole.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Adhesion_Trap_Hole` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Adhesion_Trap_Hole.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Amazoness_Archers` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Amazoness_Archers.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Anti_Raigeki` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Anti_Raigeki.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Anti_Spell` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Anti_Spell.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Arsenal_Robber` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Arsenal_Robber.cs` | can send 1 equip spell from your deck to the graveyard. |
| `Attack_and_Receive` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Attack_and_Receive.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Backfire` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Backfire.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Backup_Soldier` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Backup_Soldier.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Barrel_Behind_the_Door` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Barrel_Behind_the_Door.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Battle_Scarred` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Battle_Scarred.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Beckoning_Light` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Beckoning_Light.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Begone_Knave` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Begone_Knave.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Big_Burn` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Big_Burn.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Blast_Held_by_a_Tribute` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Blast_Held_by_a_Tribute.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Blast_with_Chain` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Blast_with_Chain.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Blasting_the_Ruins` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Blasting_the_Ruins.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Bottomless_Trap_Hole` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Bottomless_Trap_Hole.cs` | 1 cost, if enemy intends to attack for 15 or more they deal 30 damage. |
| `Burst_Breath` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Burst_Breath.cs` | 0 cost, tribute 1 dragon, all enemies take damage equal to its attack |
| `Call_of_the_Grave` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Call_of_the_Grave.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Call_of_the_Haunted` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Call_of_the_Haunted.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Castle_Walls` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Castle_Walls.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ceasefire` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Ceasefire.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Chain_Disappearance` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Chain_Disappearance.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Coffin_Seller` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Coffin_Seller.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Compulsory_Evacuation_Device` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Compulsory_Evacuation_Device.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Crush_Card_Virus` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Crush_Card_Virus.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Curse_of_Aging` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Curse_of_Aging.cs` | destroy 1 card in hand, all enemies gain 1 weaken and 1 vulnerable |
| `Curse_of_Anubis` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Curse_of_Anubis.cs` | your effect monsters can't attack and their base def is 0 this turn, enemies permanently lose 1 strength & 1 dex and gain 1 weaken. |
| `Cursed_Seal_of_the_Forbidden_Spell` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Cursed_Seal_of_the_Forbidden_Spell.cs` | 0 cost, Discard 1 spell gain 3 (4) artifact |
| `DNA_Surgery` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/DNA_Surgery.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `DNA_Transplant` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/DNA_Transplant.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `D_Tribe` | `YgoDuelistCode/Cards/Trap/Todo/Normal/D_Tribe.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dark_Mirror_Force` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Dark_Mirror_Force.cs` | 1 cost, target one enemy who intends to attack, inflict damage equal to its attack to all enemies who do not intend to attack. |
| `Dark_Spirit_of_the_Silent` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Dark_Spirit_of_the_Silent.cs` | 2 (1) cost, If at least 2 enemies intend to attack, target one enemy, negate its attack, the other attacks twice. |
| `Deal_of_Phantom` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Deal_of_Phantom.cs` | 1 cost, upgrades to 2 atk per monster in grave |
| `Destiny_Board` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Destiny_Board.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dice_Re_Roll` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Dice_Re_Roll.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Dragon_s_Rage` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Dragon_s_Rage.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Draining_Shield` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Draining_Shield.cs` | 0 cost, gain 1 life for every enemy that intends to attack you this turn. |
| `Embodiment_of_Apophis` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Embodiment_of_Apophis.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Enchanted_Javelin` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Enchanted_Javelin.cs` | if an enemy intends to attack, heal for 1/9th of the damage it would deal. |
| `Energy_Drain` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Energy_Drain.cs` | Gains 2atk per card in your hand. |
| `Escalation_of_the_Monarchs` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Escalation_of_the_Monarchs.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fatal_Abacus` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Fatal_Abacus.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fiend_Comedian` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Fiend_Comedian.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Fiend_s_Hand_Mirror` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Fiend_s_Hand_Mirror.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Gift_of_The_Mystical_Elf` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Gift_of_The_Mystical_Elf.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Gorgon_S_Eye` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Gorgon_S_Eye.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Graverobber` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Graverobber.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Graverobber_s_Retribution` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Graverobber_s_Retribution.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Gryphon_Wing` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Gryphon_Wing.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Hidden_Spellbook` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Hidden_Spellbook.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Human_Wave_Tactics` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Human_Wave_Tactics.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Jar_of_Greed` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Jar_of_Greed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Judgment_of_Anubis` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Judgment_of_Anubis.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Just_Desserts` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Just_Desserts.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Kunai_with_Chain` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Kunai_with_Chain.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Life_Absorbing_Machine` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Life_Absorbing_Machine.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Light_of_Judgment` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Light_of_Judgment.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Lightforce_Sword` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Lightforce_Sword.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Magic_Cylinder` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Magic_Cylinder.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Magical_Arm_Shield` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Magical_Arm_Shield.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mask_of_Weakness` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Mask_of_Weakness.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Metalmorph` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Metalmorph.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Michizure` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Michizure.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Micro_Ray` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Micro_Ray.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Mirror_Force` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Mirror_Force.cs` | 1 cost, target one enemy who intends to attack, inflict damage equal to its attack to all enemies who do not intend to attack. |
| `Mirror_Wall` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Mirror_Wall.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Narrow_Pass` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Narrow_Pass.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Negate_Attack` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Negate_Attack.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Nightmare_Wheel` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Nightmare_Wheel.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ninjitsu_Art_of_Transformation` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Ninjitsu_Art_of_Transformation.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Numinous_Healer` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Numinous_Healer.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Nutrient_Z` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Nutrient_Z.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ojama_Trio` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Ojama_Trio.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ominous_Fortunetelling` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Ominous_Fortunetelling.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ordeal_of_a_Traveler` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Ordeal_of_a_Traveler.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Order_to_Smash` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Order_to_Smash.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Pharaoh_s_Treasure` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Pharaoh_s_Treasure.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Physical_Double` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Physical_Double.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Pitch_Black_Power_Stone` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Pitch_Black_Power_Stone.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Pyro_Clock_of_Destiny` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Pyro_Clock_of_Destiny.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Raigeki_Break` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Raigeki_Break.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ray_of_Hope` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Ray_of_Hope.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Reckless_Greed` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Reckless_Greed.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Reinforcements` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Reinforcements.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Reverse_Trap` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Reverse_Trap.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Ring_of_Destruction` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Ring_of_Destruction.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rite_of_Spirit` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Rite_of_Spirit.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rivalry_of_Warlords` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Rivalry_of_Warlords.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Robbin_Goblin` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Robbin_Goblin.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Robbin_Zombie` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Robbin_Zombie.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Rope_of_Life` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Rope_of_Life.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Sakuretsu_Armor` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Sakuretsu_Armor.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Secret_Barrel` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Secret_Barrel.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Self_Destruct_Button` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Self_Destruct_Button.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Skull_Dice` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Skull_Dice.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Skull_Invitation` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Skull_Invitation.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Skull_Lair` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Skull_Lair.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Solar_Ray` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Solar_Ray.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Solemn_Judgment` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Solemn_Judgment.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Solemn_Wishes` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Solemn_Wishes.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Soul_Demolition` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Soul_Demolition.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Soul_Resurrection` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Soul_Resurrection.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Spell_Shield_Type_8` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Spell_Shield_Type_8.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Statue_of_the_Wicked` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Statue_of_the_Wicked.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_First_Monarch` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/The_First_Monarch.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_First_Sarcophagus` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/The_First_Sarcophagus.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `The_Spell_Absorbing_Life` | `YgoDuelistCode/Cards/Trap/Todo/Normal/The_Spell_Absorbing_Life.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Time_Machine` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Time_Machine.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Tower_of_Babel` | `YgoDuelistCode/Cards/Trap/Todo/Continuos/Tower_of_Babel.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Trap_Hole` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Trap_Hole.cs` | 1 cost, if enemy intends to attack for 15 or more they deal 30 damage. |
| `Trap_of_Board_Eraser` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Trap_of_Board_Eraser.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Waboku` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Waboku.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `White_Hole` | `YgoDuelistCode/Cards/Trap/Todo/Normal/White_Hole.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Widespread_Ruin` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Widespread_Ruin.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |
| `Windstorm_of_Etaqua` | `YgoDuelistCode/Cards/Trap/Todo/Normal/Windstorm_of_Etaqua.cs` | +2, +3 if 15+, +4 if 22+, +5 if 29+ |

## Stub / template-only cards (constructor-only)

Total: **534** — listed in `card_inventory_data.json` under `stubs`.

## Fusion / Ritual stub appendix (materials-only or empty ritual body)

- **FusionMonster stubs (materials in C# only):** 75
- **RitualMonster stubs:** 20
