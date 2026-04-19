Make tier list of fusion monsters
Tweak PackWeights for every fusion so better ones / less niche ones appear more often in packs
Tweak PackWeights so substitute fusion mat monsters / fusion spells etc appear more often.
Buff shit ones


Make these Fusion Monsters CardRarity.Uncommon - Black Skull Dragon, Dark Flare Knight, Dark Paladin, Ojama King, 

Make these Fusion Monsters CardRarity.Rare - Dragon Master Knight, Five-Headed Dragon, Meteor Black Dragon, Master of Oz



Make BulkBundled = true
Bulk Bundles system where any cards within the same rarity sharing tags can be chosen using deterministic
card selection rng to be bundled with that card, these are displayed & handled the same way as regular bundles at the shop.
For card packs any extra bulk cards will go towards a BonusBulkCards list as bonus cards added indepedent of card pack size, 

They do not replace any of the other cards in the pack. 

For card packs of size 4 or below the system will only choose and give you / let you receive up to 2 BonusBulkCards with the highest rarities, 
if there are ties among BonusBulkCards with the Highest Raritys to include, it will use deterministic rng to select them. (ex: if you had a BonusBulkCards list of 1 Rare, 2 Uncommons, and 1 Common} 
You would get the Rare + a deterministic RNG chosen one of the Uncommons.

For card packs of size 5 or above the system will only choose and give you / let you receive up to 3 BonusBulkCards with the highest rarities, 
if there are ties among BonusBulkCards with the Highest Raritys to include, it will use deterministic rng to select them. (ex: if you had a BonusBulkCards list of 2 Rares, 3 Uncommons} 
You would get the 2 Rares + a deterministic RNG chosen one of the Uncommons.


Cards with public override BulkBundled = true
Mokey Mokey King, Amphibious Bugroth, Aqua Dragon, Dragoness the Wicked Knight, Kamionwizard, Man-Eating Black Shark, Mavelus, Barox, Bickuribox, Bracchio-raidus,
Crimson Sunbird, Cyber Saurus, Darkfire Dragon, Deepsea Shark, Empress Judge, Flame Swordsman, Flower Wolf, Giltia the D Knight, Great Mammoth of Goldfine, Humanoid Worm Drake, Kaiser Dragon, Kaminari Attack,
Karbonala Warrior, KWagar Hercules, Marine Beast, Metal Dragon, Musician King, Mystical Sand, Pragtical, Punished Eagle, Rabid Horseman, Rare Fish, Roaring Ocean Snake, Rose Spectre of Dunn, Sanwitch,
Skelgon, Skull Knight, Skullbird, Soul Hunter, Thousand Dragon, Vermillion Sparrow, Warrior of Tradition. 

Uncommon Cards with public override BulkBundled = true
Gaia the Dragon Champion, Skull Knight, Chimera the Flying Mythical Beast, Dark Blade the Dragon Knight, King Dragun, Gatling Dragon, Aligator's Sword Dragon, Labyrinth Tank, St Joan, Fusionist, Charubin the Fire Knight, Flame Ghost, Zombie Warrior.



For fusion monsters level 1 to 8 implement these energy cost and upgrade scaling changes.

Fusion monster with <= 10 ATK should be 0 energy cost ATK
Fusion monster with <= 10 DEF should be 0 energy cost DEF

Fusion monster with 11 to 12 ATK should be 1 energy cost ATK upgrading to 0 energy cost 14 ATK
Fusion monster with 11 to 12 DEF should be 1 energy cost DEF upgrading to 0 energy cost 14 DEF

Fusion monster with 13 ATK should be 1 energy cost ATK upgrading to gain +4 ATK
Fusion monster with 13 DEF should be 1 energy cost DEF upgrading to gain +4 DEF

Fusion monster with 14 to 15 ATK should be 2 energy cost ATK upgrading to 1 energy cost 19 ATK
Fusion monster with 14 to 15 DEF should be 2 energy cost DEF upgrading to 1 energy cost 19 DEF 

Fusion monster with 16 ATK should be 2 energy cost ATK upgrading to 1 energy cost 20 ATK
Fusion monster with 16 ATK should be 2 energy cost DEF upgrading to 1 energy cost 20 DEF 

Fusion monster with 17 to 18 ATK should be 2 energy cost ATK upgrading to 1 energy cost 21 ATK
Fusion monster with 17 to 18 ATK should be 2 energy cost DEF upgrading to 1 energy cost 21 DEF 




