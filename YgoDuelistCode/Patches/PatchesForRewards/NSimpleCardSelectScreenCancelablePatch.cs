using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRewards;

/// <summary>
/// Vanilla <see cref="NSimpleCardSelectScreen"/> never enables the close button for <see cref="CardSelectorPrefs.Cancelable"/>.
/// Wire <c>%Close</c> or <c>%Back</c> to cancel the selection task so pack reward deck/side steps can go back.
/// </summary>
[HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen._Ready))]
public static class NSimpleCardSelectScreenCancelablePatch
{
    private static readonly FieldInfo PrefsField =
        AccessTools.Field(typeof(NSimpleCardSelectScreen), "_prefs")!;

    private static readonly FieldInfo CompletionField =
        AccessTools.Field(typeof(NCardGridSelectionScreen), "_completionSource")!;

    [HarmonyPostfix]
    public static void Postfix(NSimpleCardSelectScreen __instance)
    {
        var prefs = (CardSelectorPrefs)PrefsField.GetValue(__instance)!;
        if (!prefs.Cancelable)
            return;

        NBackButton? close = __instance.GetNodeOrNull<NBackButton>("%Close")
            ?? __instance.GetNodeOrNull<NBackButton>("%Back");
        if (close == null)
            return;

        var tcs = (TaskCompletionSource<IEnumerable<CardModel>>)CompletionField.GetValue(__instance)!;
        close.Enable();
        close.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetCanceled();
                NOverlayStack.Instance?.Remove(__instance);
            }));
    }
}
