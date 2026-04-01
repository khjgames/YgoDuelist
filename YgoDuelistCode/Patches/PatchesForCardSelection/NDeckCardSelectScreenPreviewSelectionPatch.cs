using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Trunk/side relic editor: skip the second-step preview grid — main Confirm (or selecting up to max) finishes the choice immediately.
/// Otherwise: vanilla preview calls <c>UpdateVisuals(selectedCard.Pile.Type, ...)</c>, but trunk/side cards use piles from
/// <see cref="PlayerRunTrunk"/> / <see cref="PlayerRunSideDeck"/> that are not on the player's <c>Piles</c> collection, so
/// <see cref="CardModel.Pile"/> is null. The grid uses <see cref="PileType.None"/>; preview must match.
/// </summary>
[HarmonyPatch]
public static class NDeckCardSelectScreenPreviewSelectionPatch
{
    private static readonly MethodInfo? CheckIfSelectionCompleteMethod =
        AccessTools.DeclaredMethod(typeof(NDeckCardSelectScreen), "CheckIfSelectionComplete", Type.EmptyTypes);

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() =>
        AccessTools.DeclaredMethod(typeof(NDeckCardSelectScreen), "PreviewSelection", Type.EmptyTypes)!;

    [HarmonyPrefix]
    private static bool PreviewSelectionReplacement(NDeckCardSelectScreen __instance)
    {
        if (TrunkSideDeckGuiService.SkipDeckSelectPreviewLayer)
        {
            CheckIfSelectionCompleteMethod?.Invoke(__instance, null);
            return false;
        }

        var t = Traverse.Create(__instance);
        Control previewContainer = t.Field<Control>("_previewContainer").Value;
        Control previewCards = t.Field<Control>("_previewCards").Value;
        NBackButton closeButton = t.Field<NBackButton>("_closeButton").Value;
        NCardGrid grid = t.Field<NCardGrid>("_grid").Value;
        NBackButton previewCancelButton = t.Field<NBackButton>("_previewCancelButton").Value;
        NConfirmButton previewConfirmButton = t.Field<NConfirmButton>("_previewConfirmButton").Value;
        HashSet<CardModel> selectedCards = t.Field<HashSet<CardModel>>("_selectedCards").Value;

        __instance.GetViewport().GuiReleaseFocus();
        previewContainer.Visible = true;
        previewContainer.MouseFilter = Control.MouseFilterEnum.Stop;
        closeButton.Disable();
        grid.SetCanScroll(canScroll: false);
        previewCancelButton.Enable();
        previewConfirmButton.Enable();
        foreach (CardModel selectedCard in selectedCards)
        {
            grid.UnhighlightCard(selectedCard);
            NCard nCard = NCard.Create(selectedCard);
            NPreviewCardHolder child = NPreviewCardHolder.Create(nCard, showHoverTips: true, scaleOnHover: false);
            previewCards.AddChildSafely(child);
            PileType pileForVisual = selectedCard.Pile?.Type ?? PileType.None;
            nCard.UpdateVisuals(pileForVisual, CardPreviewMode.Normal);
        }

        Callable.From(delegate
        {
            previewCards.PivotOffset = previewCards.Size / 2f;
            float num = 1f;
            if (selectedCards.Count > 6)
                num = 0.55f;
            else if (selectedCards.Count > 3)
                num = 0.8f;
            previewCards.Scale = Vector2.One * num;
        }).CallDeferred();

        return false;
    }
}
