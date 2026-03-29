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
(making it a lucky pack with bonus cards, the grown pack size has a max limit of 10 after which it would be allowed to get rid of random non-bundled rare cards until they are left with 10).  

Bundle mates first take slots by replacing **non-bundled** cards in **lowest rarity order** (Commons, then Uncommons); rares in the bundle are never overwritten by that replacement step. Only if the pack still has more than 10 cards after bundle resolution do we remove **random non-bundled rares** until the count is 10. With at most **4** bundle cards and at most **6** rolled slots before growth, the pack cannot exceed 10 in normal use; the rare-trim step is the documented cap, not a separate “edge case” path.

Each **Rare** card removed in that trim (to get back to 10) grants **+1 `OwedRareCardVouchers`**—the same run-state counter used when a Rare slot cannot be filled from the pool. That way, if bundle growth forces rares off the pack, the player is credited for those lost rares on a later pack roll. Demotions from an empty Rare pool already grant a voucher in **GetRandomCardForSlot**; trim vouchers are **only** for rares that were actually in the pack before trim.

A pack cannot pull cards from multiple different bundles, once a card from a bundle has been rng selected,  
all the remaining non-bundled cards in the pack are chosen from cards without the Bundled tag.  

Packs can have 1-3 tags, normally just 1, sometimes 2, 3 is rare. They will pull card pools from any cards with one of their tags.    

When players get a card reward they will be offered 3 packs.   

**For each of the three packs independently**, roll how many tags that pack has (same probabilities for each pack):

48% chance of being a single tag pack, 37% chance of being a double tag pack, 15% chance of being a tripple tag pack.  

The three packs **share** the same working tag lists: whenever a tag is chosen for any pack, it is removed from the lists so later packs cannot reuse it.

```csharp
// Tag lists — do not include None; None is not a rolled theme.
PossibleTags[] = {
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
    YgoCardPackTags.WinCon,
    YgoCardPackTags.God
}
```

For choosing a packs first tag always choose at random one of the possible tags then remove it from the list.  
For choosing a packs second or third tags choose at random from a combined list of PossibleTags and PossibleSubTags and remove whatever you chose from the list.   

Then for your chosen pack size, for example for all packs are 6 cards. // NumPackCardSlots = 6

First roll every slot in the card pack for its rarity 
if OwedRareCardVouchers >= 1 assign the current slot to CardRarity.Rare and then OwedRareCardVouchers -= 1;
otherwise, roll for that slots rarity using functions from the base game class CardRarityOdds.cs  

`OwedRareCardVouchers` is **run state**: it persists across save/load (stored on the YGO save trailer marker with other run fields).

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
-> Iterate through the pool of all cards of that slots rarity -> CalculateIndividualCardWeight(card) for that card to build the weight list. **Immediately before** the weighted RNG consumes each weight, apply `max(1, weight)` so no card’s pick weight is below 1.

-> If the players chosen pack doesn't contain any cards of the CardRarity.Rare rarity and it was supposed to award a rare card according to RolledCardRarities, give them an Uncommon instead (if they don't have any Uncommons, give a Common instead) and record that they gain +1 OwedRareCardVouchers.

-->> In CalculateIndividualCardWeight you would do  
int BaseWeight = 20;  // The higher you make base weight the less impact other systems make on a cards odds.

int CurrentWeight = BaseWeight;

Subtract **2** from the weight for each copy of that **individual card** in the **trunk**, then clamp:  
`CurrentWeight = max(1, BaseWeight - 2 * trunkCopiesOfThisCardId)`  
(before adding related-card bonuses and duplicate-in-pack scaling below).

Cards also have a have a RelatedCards system, where they can define a list of other related cards who'se weighted rng chance to obtain you would like to increase. 

Every card in your deck's related cards have their weighted chances increase by +2, every card in your side deck's related cards have their weighted chances increased by 1. 

That means you have to iterate through every ygo card in your deck and side deck and see if any of them give RelatedCards weight bonuses to this individual card.

It is also possible to get multiple copies of the same card from a pack but the weighted odds for a card goes down to one third of its odds when chosen, meaning they are 1/3rd as likely, then 1/9th as likely to be chosen within the same pack.  

Let `k` = number of copies of this card **already** in `ChosenPackCards` when evaluating weight for another copy (`k` is 0 for the first copy). Then:

```csharp
CurrentWeight = CurrentWeight / Pow(3, k);   // k == 0 => unchanged; k == 1 => /3; k == 2 => /9; ...

CurrentWeight = max(1, CurrentWeight); // Minimum card weight scales down to 1  
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

### Trunk / Side Deck relic (outside of pack flow)

After cards land in Trunk or Side, the **Trunk / Side Deck** starter relic opens **one** full-screen card grid (vanilla `NSimpleCardSelectScreen`)—no separate menu window. The last-used **page** (Trunk edit, Side edit, or Split editor) is restored when possible.

- **Trunk page:** move Trunk → Side. Top buttons: *Edit side deck*, *Split editor*.
- **Side page:** move Side → Trunk. Top buttons: *Edit trunk*, *Split editor*.
- **Split page:** one combined grid; each selected card **swaps** piles. Top buttons: *Edit trunk*, *Edit side deck*.

Relic click again closes the grid **without** applying. Remaining pack-system items (tag pools, bundle rules, shop/event integration, etc.) are specified above and are implemented separately from this UI where noted.
