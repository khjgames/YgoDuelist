I have 1000 + cards from old school yugioh games like World Championship 2005 .

The plan is to have all of my cards have the ability to have multiple bit flag/enum Tags,  
And then I will have different card packs that have possible card pools of cards with a certain Tag.  

So for example I want to be able to go through my cards and add tags like "Ocean", "Dragon", "Zombie",  
and then I have thematic card packs that can draw upon all cards tagged with that theme,   

Card packs will be awarded in different pack sizes  2-6 cards. with rarities for Common, Uncommon, Rare cards.  
So it is extremely unlikely to open a card pack and get more than 1-2 rares but getting 2-3 commons would be very likely.   

I'm gonna implement a side-deck which is just trunk cards you probably want to run but not yet,  
so you don't have to go digging for them later.

So at minimum you always get 2 cards instead of 1 from card rewards, and sometimes the packs will be bigger, also shops and events exist.  
basically, 2, sometimes 3 or 4, 5 and 6 are more special scenario / occasion sizes

I also want to have a bundle system with certain cards , wherein RNG choosing any one of the bundled cards would guarantee the other bundled cards are also awarded in that pack, overwriting lowest rarity non bundled cards in the packs slots to make room if necessary.   

Bundles can be up to 4 cards but most will only be 2 or 3.  
(So if you were originally rng rolled to get 4 cards in a pack, 2 commons, 1 Uncommon, and 1 rare),  
but one of the cards you rolled among those was part of a 3 card bundle,  
say one of the commons, it would replace the other common and Uncommon with the cards in the bundle.)   

The bundle system cannot overwrite rare cards, that pack would grow to a minimum size able to accomodate the bundle & the rare cards  
(making it a lucky pack with bonus cards, the grown pack size has a max limit of 10 after which it would be allowed to get rid of random non-bundled rare cards until they are left with 10)  

A pack cannot pull cards from multiple different bundles, once a card from a bundle has been rng selected,  
all the remaining non-bundled cards in the pack are chosen from cards without the Bundled tag.  

Packs can have 1-3 tags, normally just 1, sometimes 2, 3 is rare. They will pull card pools from any cards with one of their tags.    

When players get a card reward they will be offered 3 packs.   

First you decide the number of tags for all 3 packs. How this works is.  

48% chance of being a single tag pack, 37% chance of being a double tag pack, 15% chance of being a tripple tag pack.  

```csharp
// Tag lists are set right after deciding
PossibleTags[] = {
    YgoCardPackTags.None,
    YgoCardPackTags.Earth,
    YgoCardPackTags.Water,
    YgoCardPackTags.Wind,
    YgoCardPackTags.Fire,
    YgoCardPackTags.Dark,
    YgoCardPackTags.Light,
    YgoCardPackTags.Fusion,
    YgoCardPackTags.Ritual,
    YgoCardPackTags.Ocean,
    YgoCardPackTags.Insect,
    YgoCardPackTags.Machine,
    YgoCardPackTags.Dragon,
    YgoCardPackTags.Zombie,
    YgoCardPackTags.Fiend,
    YgoCardPackTags.Spellcaster,
    YgoCardPackTags.Warrior,
    YgoCardPackTags.Heal,
    YgoCardPackTags.Draw,
    YgoCardPackTags.Chance,
    YgoCardPackTags.Burn,
    YgoCardPackTags.Normal,
    YgoCardPackTags.Spell,
    YgoCardPackTags.Trap
}
PossibleSubTags[] = {
    YgoCardPackTags.WinCondition,
    YgoCardPackTags.God
}
```

For choosing a packs first tag always choose at random one of the possible tags then remove it from the list.  
For choosing a packs second or third tags choose at random from a combined list of PossibleTags and PossibleSubTags and remove whatever you chose from the list.   

Then for your chosen pack size, for example for all packs are 6 cards. // NumPackCardSlots = 6

First roll every slot in the card pack for its rarity 
if OwedRareCardVouchers >= 1 assign the current slot to CardRarity.Rare and then OwedRareCardVouchers -= 1;
otherwise, roll for that slots rarity using functions from the base game class CardRarityOdds.cs  
```csharp
CardRarity RolledCardRarities[] = {CardRarity.Common, CardRarity.Uncommon, CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare, CardRarity.Common}
```
The rarities of the slots are rolled once then shared by the 3 card packs (so they all contain the same distribution of commons / Uncommons / rares), 

When selecting the cards for a pack. The pseudocode for the logic is like this. ->  

PackCardPool[] = GetPoolOfAllCardsWithAnyOfTheseTags(PackTags)

```csharp

ChosenPackCards = [];

EncounteredBundledCards = [];

for (i = 0, i < NumPackCardSlots; i++;) GetRandomCardForSlot(i);
```

-->> in GetRandomCardForSlot()   
-> Get the pool of all cards of that slots rarity.  
-> Iterate through the pool of all cards of that slots rarity -> CalculateIndividualCardWeight(card) for that card to build

-> If the players chosen pack doesn't contain any cards of the CardRarity.Rare rarity and it was supposed to award a rare card according to RolledCardRarities, give them an Uncommon instead (if they don't have any Uncommons, give a Common instead) and record that they gain +1 OwedRareCardVouchers.

-->> In CalculateIndividualCardWeight you would do  
int BaseWeight = 20;  // The higher you make base weight the less impact other systems make on a cards odds.

int CurrentWeight = BaseWeight;

you factor in -2 weight for each copy of that individual card in the trunk. (so CurrentWeight = math.max(1,BaseWeight);

Cards also have a have a RelatedCards system, where they can define a list of other related cards who'se weighted rng chance to obtain you would like to increase. 

Every card in your deck's related cards have their weighted chances increase by +2, every card in your side deck's related cards have their weighted chances increased by 1. 

That means you have to iterate through every ygo card in your deck and side deck and see if any of them give RelatedCards weight bonuses to this individual card.

It is also possible to get multiple copies of the same card from a pack but the weighted odds for a card goes down to one third of its odds when chosen, meaning they are 1/3rd as likely, then 1/9th as likely to be chosen within the same pack.   
So for the number of times this card shows up in ChosenPackCards do   
```csharp
NumCopiesOfThisCardInChosenPackCards = CountNumCopiesOfThisCardInChosenPackCards(); // write this.

CurrentWeight = CurrentWeight / (1 * (3 * NumCopiesOfThisCardInChosenPackCards))  
  
CurrentWeight = math.max(1, CurrentWeight); // Minimum card weight scales down to 1  
```  

The player can either skip card packs or choose to take one of them, 

if they choose to take a card pack their minimum deck size will increase by 1.

Then they will open up a grid selection screen showing all the cards they just got from the pack.

They can select which cards they would like to put in their deck, confirm, then choose which cards to put in their side deck.

The rest will go to their trunk. These selections use our Cancelable grid selection interface, 

if they cancel out of choosing which cards they would like to add to their deck they go back a UI step 

to deciding which pack they want or whether to skip.

If they cancel during choosing which cards they would like to put in their side deck they go back a UI step to deciding which 

cards from the pack they want in their deck.

The minimum deck size increase and new cards piles are only set after they have confirmed the side deck cards.

