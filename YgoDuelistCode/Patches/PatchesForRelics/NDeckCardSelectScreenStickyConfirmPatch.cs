using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Trunk/side/split editor: main Confirm applies moves but keeps <see cref="NDeckCardSelectScreen"/> open until Close or relic dismiss
/// (await <see cref="NCardGridSelectionScreen.CardsSelected"/> still tied to those exits only).
/// </summary>
[HarmonyPatch]
public static class NDeckCardSelectScreenStickyConfirmPatch
{
    private static readonly MethodInfo? RefreshConfirmButtonVisibilityMethod =
        AccessTools.DeclaredMethod(typeof(NDeckCardSelectScreen), "RefreshConfirmButtonVisibility", Type.EmptyTypes);

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() =>
        AccessTools.DeclaredMethod(typeof(NDeckCardSelectScreen), "CheckIfSelectionComplete", Type.EmptyTypes)!;

    [HarmonyPrefix]
    private static bool Prefix(NDeckCardSelectScreen __instance)
    {
        if (!TrunkSideDeckGuiService.SkipDeckSelectPreviewLayer || TrunkSideDeckGuiService.EditorSessionPlayer == null)
            return true;

        var t = Traverse.Create(__instance);
        HashSet<CardModel> selected = t.Field<HashSet<CardModel>>("_selectedCards").Value;
        CardSelectorPrefs prefs = t.Field<CardSelectorPrefs>("_prefs").Value;
        NCardGrid mainGrid = t.Field<NCardGrid>("_grid").Value;

        if (selected.Count < prefs.MinSelect)
            return true;

        if (selected.Count == 0)
            return false;

        List<CardModel> picked = new List<CardModel>(selected);
        TrunkSideDeckGuiService.ApplyTrunkSideEditorMoves(picked);

        foreach (CardModel c in picked)
        {
            mainGrid.UnhighlightCard(c);
            if (TrunkSideDeckSplitGridState.Active && TrunkSideDeckSplitGridState.RightGrid != null)
                TrunkSideDeckSplitGridState.RightGrid.UnhighlightCard(c);
        }

        selected.Clear();
        TrunkSideDeckDeckCardSelectScreenPatch.ResyncChromeAfterPileMutation();
        RefreshConfirmButtonVisibilityMethod?.Invoke(__instance, null);

        if (prefs.Cancelable)
            t.Field<NBackButton>("_closeButton").Value.Enable();

        return false;
    }
}
