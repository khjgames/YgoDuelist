How should we handle and implement the merchant/shop.
We could technically sell packs, but I don't really like that idea as much as browsing and buying individual cards
Like, give gold some guarenteed return value, no gamba involved yk what i mean
Theres already enough pack distribution according to combats elites and bosses
So its better to give us some other / new avenues of options.
Also worth mentioning is im going to have to boost pack size by 1 across the board once I implement sealed pack visuals hiding their contents since revealed is too op and also again has you min maxxing between the three card packs instead of trying to deckbuild around themes and stuff
(So min pack size would be 3 cards instead of 2) making it much easier to get at least one card you want out of blind pick thematic tagged packs. 
The gradual tweaking of the related cards system will also help with this too though to be fair
Anyhow as for the specifics of the merchant/shop

Im thinking an individual buy card pool of 4 cards each from 4 randomly chosen tagged packs with a guarentee one of the packs themes matches one of the two most occuring tags in your deck (so if most of your decks cards are light and dark there would always be at least one light or dark pack chosen) 
So 16 cards total, and probably do 6 commons 8 uncommons and 2 rares in the merchant/shop card pool
With pricing scaling by card rarity
On the topic
I think it would be cool to let players sell cards, commons  0.5, uncommon 1, rare 5

It would give them the opportunity to clear out their trunks if they want or to scrounge/ scavenge together 10-40 gold here and there
If they are just barely short of something they want to buy
The specific gold values would have to be really low though, I don't want it to be very significant 

The merchant/shop rug would have two new GUI pages you can visit on top of the default merchant layout, 

The Sell GUI Page is a selection screen of cards in your trunk and side deck, that lets you select cards and see a running tally of their total sell price gold values, and then a confirm followed by a second prompt sell "x commons, y uncommons, and z rares" for w gold? That you have to confirm to lock it in. 

The Buy GUI Page is much like the existing merchant layout, but with the new card pool and pricing system. (16 individual cards each with their own price listed, same as the regular merchant layout, but a bit more compact / 4x4 grid like, with each pack of 4 cards for sale being in one of the four rows)

Fun ideas -> 

It would be really funny if there was an alternate act set duel academy
Act 1 slifer red, act 2 ra yellow, act 3 obelisk blue lol

Duelist Tutorials
Add a page to the menu 
Freeplay Duel

Able to deck edit save and load and play with decks against certain combats, both cool for testing and learning or redoing boss fights, also allows you to select edit save and load relic lists with it.
Ability to import and export all or individual lists too
If we do this we can much easier test cards in combats against specific enemies. This is brilliant even outside of the duelist mod.
As for the tutorials thing

It will just cover the duelists

Normal summons & monster commands with conduit.

Spell and Trap Card Zone

Special summons (monster reborn, and from hand)

Field Spells
Equip Spells
Continuous cards

Fusion summons.
Ritual summons.

Packs & Deck editing.


This is for planning adding a weighted rng selection system to the tag selection step of our implemented packs system @YgoDuelist/Game_Design/Packs_System.md 

The goal being that we will will softly push the RNG distribution such that our desired balance of tags are selected throughout the run rather than some tags never being chosen, or others being overly chosen, and in doing so we are also allowed more freedom and control over the individual tags occurance and balanced weight rate tuning, you know what I mean?

I think I should implement a balanced weight system
so whenever a pack drafts a certain tag it will add to that rolls weight either 4, 3 or 2, if the pack was a single, double, or triple tag

And so tags with higher weights have a smaller chance of being chosen and tags with lower weights have a higher chance of being chosen.


And you can specify individual weight ratios for each tag.

And the individual weight ratios influence what is considered "balanced" for that tag by modifying their final value so like.

We give all the tags a base weight ratio of like 10. 

then we do like spell = 22, trap = 24, attribute-based tags = 8, race-based tags = 12

Lets say you wanted the Spell, Trap, Normal tags to occur more often than other tags, 
maybe you wanted attribute specifics to occur less often and type theme specific to occur more often.

I also need to fix the ability to skip card packs and also the ability to go back and 
re-do selection of which cards in a card pack you want in your deck and which you want in your side deck before confirming