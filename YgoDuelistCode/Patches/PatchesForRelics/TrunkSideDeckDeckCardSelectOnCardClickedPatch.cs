using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Split editor uses two <see cref="NCardGrid"/> columns; vanilla only highlights on <c>_grid</c> (left). Re-apply highlight on the grid that actually holds the card.
/// </summary>
[HarmonyPatch(typeof(NDeckCardSelectScreen), "OnCardClicked", new[] { typeof(CardModel) })]
public static class TrunkSideDeckDeckCardSelectOnCardClickedPatch
{
    [HarmonyPostfix]
    public static void FixSplitColumnHighlight(NDeckCardSelectScreen __instance, CardModel card)
    {
        if (!TrunkSideDeckSplitGridState.Active
            || TrunkSideDeckSplitGridState.LeftGrid == null
            || TrunkSideDeckSplitGridState.RightGrid == null)
            return;

        NCardGrid left = TrunkSideDeckSplitGridState.LeftGrid;
        NCardGrid right = TrunkSideDeckSplitGridState.RightGrid;
        left.UnhighlightCard(card);
        right.UnhighlightCard(card);

        HashSet<CardModel>? selected = Traverse.Create(__instance).Field<HashSet<CardModel>>("_selectedCards").Value;
        if (selected == null || !selected.Contains(card))
            return;

        if (left.GetCardHolder(card) != null)
            left.HighlightCard(card);
        else if (right.GetCardHolder(card) != null)
            right.HighlightCard(card);
    }
}
