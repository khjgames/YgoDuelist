using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="NSimpleCardSelectScreen"/> has no back control; deck-style selects use <see cref="NDeckCardSelectScreen"/>'s
/// <c>%Close</c> <see cref="NBackButton"/>. When <see cref="CardSelectorPrefs.Cancelable"/> is true, we add the same
/// red back button so the player can cancel without closing the overlay via the X only.
/// </summary>
[HarmonyPatch]
public static class SimpleCardSelectScreenCancelBackButtonPatch
{
    public const string BackNodeName = "YgoSimpleSelectBack";

    private static readonly FieldInfo PrefsField =
        typeof(NSimpleCardSelectScreen).GetField("_prefs", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo CompletionField =
        typeof(NCardGridSelectionScreen).GetField("_completionSource", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo PeekField =
        typeof(NCardGridSelectionScreen).GetField("_peekButton", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen._Ready))]
    [HarmonyPostfix]
    private static void AfterReady(NSimpleCardSelectScreen __instance)
    {
        if (__instance.GetNodeOrNull(BackNodeName) != null)
            return;

        var prefs = (CardSelectorPrefs)PrefsField.GetValue(__instance)!;
        if (!prefs.Cancelable)
            return;

        Callable.From(() => AttachBackButton(__instance)).CallDeferred();
    }

    private static void AttachBackButton(NSimpleCardSelectScreen screen)
    {
        if (!GodotObject.IsInstanceValid(screen) || screen.GetNodeOrNull(BackNodeName) != null)
            return;

        var prefs = (CardSelectorPrefs)PrefsField.GetValue(screen)!;
        if (!prefs.Cancelable)
            return;

        var scene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/back_button"));
        var back = scene.Instantiate<NBackButton>(PackedScene.GenEditState.Disabled);
        back.Name = BackNodeName;
        back.LayoutMode = 1; // same as deck/map back instances in scenes
        screen.AddChildSafely(back);
        back.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnBackPressed(screen)));

        if (PeekField.GetValue(screen) is NPeekButton peek)
            peek.AddTargets(back);

        back.Enable();
    }

    private static void OnBackPressed(NSimpleCardSelectScreen screen)
    {
        if (!GodotObject.IsInstanceValid(screen))
            return;

        var tcs = (TaskCompletionSource<IEnumerable<CardModel>>)CompletionField.GetValue(screen)!;
        tcs.SetResult(Array.Empty<CardModel>());
        NOverlayStack.Instance?.Remove(screen);
    }

    // Declared on NCardGridSelectionScreen, not NSimpleCardSelectScreen — Harmony needs the declaring type.
    [HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.AfterOverlayShown))]
    [HarmonyPostfix]
    private static void AfterOverlayShown(NCardGridSelectionScreen __instance)
    {
        if (__instance is not NSimpleCardSelectScreen simple)
            return;
        if (simple.GetNodeOrNull(BackNodeName) is NBackButton back)
            back.Enable();
    }

    [HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.AfterOverlayHidden))]
    [HarmonyPostfix]
    private static void AfterOverlayHidden(NCardGridSelectionScreen __instance)
    {
        if (__instance is not NSimpleCardSelectScreen simple)
            return;
        if (simple.GetNodeOrNull(BackNodeName) is NBackButton back)
            back.Disable();
    }
}
