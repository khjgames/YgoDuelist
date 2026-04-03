namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// Which vanilla card-library control scene backs a custom filter toggle.
/// </summary>
public enum CardLibraryFilterToggleStyle
{
    /// <summary><see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardTypeTickbox"/> (icon).</summary>
    CardType,

    /// <summary><see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardRarityTickbox"/> (checkbox + text).</summary>
    Rarity,

    /// <summary><see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardCostTickbox"/> (cost-style label tile).</summary>
    Cost
}
