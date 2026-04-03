I want you to expand the compendium card library filtering GUI options. How you should do this is by making the lefhand side where we have the Card Type, Rarity, and Cost filters and sorts into a vertically scrolling list so we have more room to keep adding sections below Cost that we can scroll into / out of view on the left hand bar. 
Make us a new CardLibraryFilterSortingRuleCategoryGUI class that we can use to add new menu sections with their own Named sorting button like the existing CardType, Rarity, and Cost sort buttons. 
And use it to make a new sorting button & menu section YGO_Tags.
And also make us a new CardLibraryFilterSortingRuleCategoryFilterToggleGUI class that allows us to add new Toggle buttons with a settable ToggleStyle enum based on the 3 existing styles, that specifies what existing button type is used as the template for that toggle. 
ToggleStyle.CardType
ToggleStyle.Rarity,
ToggleStyle.Cost
So like ToggleStyle.Rarity would mean the created CardLibraryFilterSortingRuleCategoryFilterToggleGUI would be a button with a checkbox to the left and formattable text on the right, like the existing "Common", "Uncommon", "Rare", "Other" gui toggles in the Rarity Section.
So like ToggleStyle.Rarity would mean the created CardLibraryFilterSortingRuleCategoryFilterToggleGUI would be a button with a simple background for the text, like the existing "0", "1", "2", "3+", "X" gui toggles in the Cost Section.
And ToggleStyle.CardType would mean the created CardLibraryFilterSortingRuleCategoryFilterToggleGUI would be an image/icon button, like the existing "Attacks", "Skills", "Powers", "Status, Curse, and Quest cards" gui toggles in the CardType Section.

Using these new classes, 
make us a new CardLibraryFilterSortingRuleCategoryGUI for "YgoCardPackTags"
with new filter option CardLibraryFilterSortingRuleCategoryFilterToggleGUI Toggle buttons with ToggleStyle.Rarity for "Any",  and all of these tags  @YgoCardTypes.cs (19-50) 

"Any" and "None" by default are checked (true), and the rest are not.
"Any" would restrict displayed cards to YgoCardPackTags PackTags that isn't None, 
and the rest of the tags would restrict displayed cards to those whose YgoCardPackTags PackTags have one or more of the checked tags.

