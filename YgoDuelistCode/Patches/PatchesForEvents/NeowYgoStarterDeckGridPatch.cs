using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForEvents;

/// <summary>
/// Intercepts Neow's first <see cref="AncientEventModel.SetInitialEventState"/> for YgoDuelist: structured starter grid (<see cref="YgoStarterCardCatalog.GridSize"/>–<see cref="YgoStarterCardCatalog.MaxGridSize"/> cards, pick 9–max) then Neow blessing UI.
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "SetInitialEventState")]
public static class NeowYgoStarterDeckGridPatch
{
    private static void NeowDraftLog(string msg)
    {
        GD.Print($"[YgoDuelist NeowDraft] {msg}");
        MainFile.Logger.Info($"[NeowDraft] {msg}");
    }

    [HarmonyPrefix]
    public static bool Prefix(AncientEventModel __instance, bool isPreFinished)
    {
        if (__instance is not Neow)
            return true;

        NeowDraftLog($"SetInitialEventState enter isPreFinished={isPreFinished} ownerNull={__instance.Owner == null}");

        if (YgoNeowStarterDeckGridService.ShouldSkipStarterDraftInterceptAndClear(__instance))
        {
            NeowDraftLog("allow original: resume-after-draft guard cleared");
            return true;
        }

        if (isPreFinished)
        {
            NeowDraftLog("allow original: isPreFinished=true (loaded mid-event / skip flow)");
            return true;
        }

        if (__instance.Owner is not { } owner)
        {
            NeowDraftLog("allow original: Owner is null");
            return true;
        }

        bool isYgo = YgoPlayerRunPiles.IsYgoRunPlayer(owner);
        string charName = owner.Character?.GetType().Name ?? "null";
        NeowDraftLog($"characterType={charName} isYgoDuelistPlayer={isYgo}");
        if (!isYgo)
        {
            NeowDraftLog("allow original: not YgoDuelist character");
            return true;
        }

        NeowDraftLog($"intercepting: deckCount={owner.Deck.Cards.Count} (no skip by deck size — A20/relics/modifiers add cards before Neow)");
        NeowDraftLog("intercepting: scheduling RunDraftThenResumeNeowAsync via TaskHelper.RunSafely");
        TaskHelper.RunSafely(YgoNeowStarterDeckGridService.RunDraftThenResumeNeowAsync(__instance, isPreFinished));
        NeowDraftLog("return false: skipped vanilla SetInitialEventState (draft runs async)");
        return false;
    }
}
