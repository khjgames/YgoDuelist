standardize starter deck rng
--------------------------------
same number of spells, traps,
commons, uncommons, rares,
monsters by level band (Mon_Low 1–2, Mon_Mid 3–4, Mon_High ≥5)

it should be 19 cards (used to be 18)

Classify the Starter tagged cards into these 5 StarterCategory enums 
- Count the number of rares, uncommons, and commons within each category and store them we should be abretrieve or edit entries given the StarterCategory & CardRarity.

Spell, - BaseSpellCard
Trap,  - BaseTrapCard
Mon_Low, - BaseMonsterCard - level 1 or 2
Mon_Mid, - BaseMonsterCard - level 3 or 4
Mon_High - BaseMonsterCard - level >= 5

@YgoDuelist/YgoDuelistCode/Services/YgoStarterCardCatalog.cs 

CategoryPrecedenceOrder = { 
Spell, Trap, Mon_Low, Mon_Mid, Mon_High 
} // Shuffle & randomize this CategoryPrecedenceOrder list to any possible combination of the 5

CardRarity - StarterCardRarities = {
    CardRarity.Rare,
    CardRarity.Uncommon, CardRarity.Uncommon, 
    CardRarity.Uncommon, CardRarity.Uncommon, 
    CardRarity.Uncommon, CardRarity.Uncommon, 
    CardRarity.Common, CardRarity.Common, CardRarity.Common, 
    CardRarity.Common, CardRarity.Common, CardRarity.Common, 
    CardRarity.Common, CardRarity.Common, CardRarity.Common, 
    CardRarity.Common, CardRarity.Common, CardRarity.Common
}
// 1 rare, 6 uncommons, 12 commons, rarities selected in this order

// we need to fill our 19 selected card slots for our grid selection

// there are exactly this many Needed cards of each category, this is used by GetNextUniqueCardOfRarity to ensure we end up with the desired amount of cards.
// (matches Game_Design/Deck_Trunk_Side_System.md — sum = 19)
3 Mon_High
4 Spell
4 Trap
4 Mon_Mid
4 Mon_Low

int CPCursor = -1;

GetNextUniqueCardOfRarity(){
CPCursor +=1; if (CPCursor >= 5) CPCursor = 0;
StarterCategory SelectedCategory = CategoryPrecedenceOrder[CPCursor];
Retrieve the number of available cards of the SelectedCategory with the SelectedRarity.
If there aren't any available cards or we have already chosen enough cards of this category, 
call this function again (it will try in the next category over)
If the conditions are fine, it will grab a unique card (excluding already picked cards) from SelectedCategory of SelectedRarity and decrement the number of needed cards for SelectedCategory  SelectedRarity cards in SelectedCategory by 1 (ex: if it was selected rarity unCommon of selected category spell it would reduce the count of unCommon cards in the spell category by 1, so we know)
}


for (int i = 0;  i < 19; i++){
CardRarity SelectedRarity = StarterCardRarities[i];

GetNextUniqueCardOfRarity(SelectedRarity);

}