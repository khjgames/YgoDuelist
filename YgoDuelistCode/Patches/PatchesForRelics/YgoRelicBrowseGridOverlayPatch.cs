using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Tracks exactly one zone-relic browse session (Graveyard / Banished / Extra Deck in one flow) and the trunk/side deck editor.
/// Only overlays opened immediately after <see cref="SetPendingKind"/> are bound — vanilla card/potion/power grids never set pending, so they are never touched.
/// </summary>
public static class YgoRelicBrowseGridOverlayPatch
{
    public enum RelicGridKind
    {
        None,
        ZoneRelicView,
        TrunkSideDeckSelect
    }

    /// <summary>Set in <see cref="GraveyardRelicClickPatch"/> / <see cref="TrunkSideDeckGuiService"/> / <see cref="ZoneRelicViewGuiService"/> right before the overlay Push; cleared when the matching screen is bound.</summary>
    private static RelicGridKind _pendingKind;

    private static NSimpleCardSelectScreen? _relicZoneBrowseScreen;

    private static NDeckCardSelectScreen? _activeTrunkSideDeckScreen;

    private static readonly FieldInfo? DeckSelectedCardsField =
        AccessTools.Field(typeof(NDeckCardSelectScreen), "_selectedCards");

    private static readonly FieldInfo? CompletionSourceField =
        AccessTools.Field(typeof(NCardGridSelectionScreen), "_completionSource");

    private static readonly FieldInfo? OverlayStackOverlaysField =
        AccessTools.Field(typeof(NOverlayStack), "_overlays");

    public static void SetPendingKind(RelicGridKind kind) => _pendingKind = kind;

    public static void ClearPendingKind() => _pendingKind = RelicGridKind.None;

    /// <summary>True while <see cref="RelicGridKind.ZoneRelicView"/> is pending bind to the next pushed <see cref="NSimpleCardSelectScreen"/> (dedupes duplicate relic clicks before the overlay attaches).</summary>
    public static bool IsZoneViewGridOpenInFlight() => _pendingKind == RelicGridKind.ZoneRelicView;

    public static void RegisterActiveTrunkSideDeckScreen(NDeckCardSelectScreen deck)
    {
        if (deck == null || !GodotObject.IsInstanceValid(deck))
            return;
        _activeTrunkSideDeckScreen = deck;
    }

    public static void CompleteActiveTrunkSideNavigate(TrunkSideDeckEditorPage targetPage)
    {
        if (_activeTrunkSideDeckScreen == null || !GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
            return;
        TrunkSideDeckEditorSession.RequestNavigateTo(targetPage);
        if (_activeTrunkSideDeckScreen != null && GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
            ClearTrunkSideDeckScreenEmpty(_activeTrunkSideDeckScreen);
    }

    /// <summary>Ends the current zone simple grid with an empty result so <see cref="ZoneRelicViewGuiService"/> can advance to <paramref name="targetPage"/>.</summary>
    public static void CompleteActiveZoneViewNavigate(ZoneRelicViewPage targetPage)
    {
        NSimpleCardSelectScreen? screen = ResolveZoneViewSimpleScreen();
        if (screen == null)
            return;
        ZoneRelicViewSession.RequestNavigateTo(targetPage);
        DismissRelicZoneSimpleScreen(screen);
    }

    /// <summary>Closes the zone viewer only when <paramref name="clickedPage"/> matches <see cref="ZoneRelicViewSession.ActivePage"/> (same-relic toggle).</summary>
    public static bool TryToggleCloseZoneView(ZoneRelicViewPage clickedPage)
    {
        if (ZoneRelicViewSession.ActivePage != clickedPage)
            return false;
        ZoneRelicViewSession.ClearNavigateRequest();
        NSimpleCardSelectScreen? screen = ResolveZoneViewSimpleScreen();
        if (screen == null)
            return false;
        DismissRelicZoneSimpleScreen(screen);
        return true;
    }

    /// <summary>
    /// Prefer finding the open <see cref="NSimpleCardSelectScreen"/> whose bottom prompt matches <see cref="ZoneRelicViewSession.ActivePage"/> (same keys as
    /// <c>relics.json</c> <c>*.selectionScreenPrompt</c>); then cached ref; then Peek while the zone session is running.
    /// </summary>
    private static NSimpleCardSelectScreen? ResolveZoneViewSimpleScreen()
    {
        string expected = GetFormattedZoneSelectionPrompt(ZoneRelicViewSession.ActivePage);
        if (!string.IsNullOrEmpty(expected))
        {
            NSimpleCardSelectScreen? byPrompt = FindSimpleCardSelectWithBottomPromptText(expected);
            if (byPrompt != null)
            {
                _relicZoneBrowseScreen = byPrompt;
                return byPrompt;
            }
        }

        if (_relicZoneBrowseScreen != null && GodotObject.IsInstanceValid(_relicZoneBrowseScreen))
            return _relicZoneBrowseScreen;
        if (!ZoneRelicViewGuiService.IsViewerSessionRunning())
            return null;
        if (NOverlayStack.Instance?.Peek() is not NSimpleCardSelectScreen peek)
            return null;
        if (!GodotObject.IsInstanceValid(peek))
            return null;
        _relicZoneBrowseScreen = peek;
        return peek;
    }

    /// <summary>Matches <c>YgoDuelist/localization/*/relics.json</c> <c>YGODUELIST-*_RELIC.selectionScreenPrompt</c>.</summary>
    private static string GetFormattedZoneSelectionPrompt(ZoneRelicViewPage page)
    {
        string key = page switch
        {
            ZoneRelicViewPage.Graveyard => "YGODUELIST-GRAVEYARD_RELIC.selectionScreenPrompt",
            ZoneRelicViewPage.Banished => "YGODUELIST-BANISHED_RELIC.selectionScreenPrompt",
            ZoneRelicViewPage.ExtraDeck => "YGODUELIST-EXTRA_DECK_RELIC.selectionScreenPrompt",
            _ => ""
        };
        if (string.IsNullOrEmpty(key))
            return "";
        return new LocString("relics", key).GetFormattedText();
    }

    private static NSimpleCardSelectScreen? FindSimpleCardSelectWithBottomPromptText(string expectedFormatted)
    {
        if (NOverlayStack.Instance == null || OverlayStackOverlaysField == null)
            return null;
        if (OverlayStackOverlaysField.GetValue(NOverlayStack.Instance) is not List<IOverlayScreen> overlays)
            return null;

        string normExpected = NormalizePromptForCompare(expectedFormatted);
        if (string.IsNullOrEmpty(normExpected))
            return null;

        for (int i = overlays.Count - 1; i >= 0; i--)
        {
            if (overlays[i] is not NSimpleCardSelectScreen simple)
                continue;
            if (!GodotObject.IsInstanceValid(simple))
                continue;
            string? label = ReadSimpleCardSelectBottomPromptText(simple);
            if (string.IsNullOrEmpty(label))
                continue;
            if (string.Equals(NormalizePromptForCompare(label), normExpected, StringComparison.Ordinal))
                return simple;
        }

        return null;
    }

    private static string? ReadSimpleCardSelectBottomPromptText(NSimpleCardSelectScreen screen)
    {
        if (!GodotObject.IsInstanceValid(screen))
            return null;
        try
        {
            Control? bottomText = screen.GetNodeOrNull<Control>("%BottomText");
            MegaRichTextLabel? label = bottomText?.GetNodeOrNull<MegaRichTextLabel>("%BottomLabel");
            return label?.Text;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizePromptForCompare(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        string stripped = StripBbCode(s).Trim();
        return Regex.Replace(stripped, @"\s+", " ");
    }

    private static string StripBbCode(string s) =>
        Regex.Replace(s, @"\[[^\]]*\]", "", RegexOptions.CultureInvariant);

    public static bool TryToggleClose(RelicGridKind relicKind)
    {
        switch (relicKind)
        {
            case RelicGridKind.TrunkSideDeckSelect:
                if (_activeTrunkSideDeckScreen == null || !GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
                    return false;
                TrunkSideDeckEditorSession.ClearNavigateRequest();
                ClearTrunkSideDeckScreenEmpty(_activeTrunkSideDeckScreen);
                return true;

            default:
                return false;
        }
    }

    public static void CloseAnyActiveBrowseGrid()
    {
        if (_activeTrunkSideDeckScreen != null && GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
        {
            TrunkSideDeckEditorSession.ClearNavigateRequest();
            ClearTrunkSideDeckScreenEmpty(_activeTrunkSideDeckScreen);
        }

        NSimpleCardSelectScreen? zone = ResolveZoneViewSimpleScreen();
        if (zone != null)
        {
            ZoneRelicViewSession.ClearNavigateRequest();
            DismissRelicZoneSimpleScreen(zone);
        }

        _relicZoneBrowseScreen = null;
    }

    private static void DismissRelicZoneSimpleScreen(NSimpleCardSelectScreen screen)
    {
        if (!GodotObject.IsInstanceValid(screen))
            return;
        if (CompletionSourceField?.GetValue(screen) is TaskCompletionSource<IEnumerable<CardModel>> tcs)
        {
            if (!tcs.TrySetResult(Array.Empty<CardModel>()))
                tcs.TrySetCanceled();
        }

        NOverlayStack.Instance?.Remove(screen);
    }

    private static void ClearTrunkSideDeckScreenEmpty(NDeckCardSelectScreen screen)
    {
        if (DeckSelectedCardsField?.GetValue(screen) is ISet<CardModel> set)
            set.Clear();
        if (CompletionSourceField?.GetValue(screen) is TaskCompletionSource<IEnumerable<CardModel>> tcs)
            tcs.SetResult(Array.Empty<CardModel>());
        NOverlayStack.Instance?.Remove(screen);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(NOverlayStack), nameof(NOverlayStack.Push))]
    private static void AfterOverlayPush(IOverlayScreen screen)
    {
        if (_pendingKind == RelicGridKind.None)
            return;

        if (_pendingKind == RelicGridKind.TrunkSideDeckSelect)
        {
            if (screen is NDeckCardSelectScreen deck)
            {
                _activeTrunkSideDeckScreen = deck;
                _pendingKind = RelicGridKind.None;
            }

            return;
        }

        if (screen is not NSimpleCardSelectScreen simple)
            return;

        if (_pendingKind == RelicGridKind.ZoneRelicView)
        {
            if (_relicZoneBrowseScreen != null
                && GodotObject.IsInstanceValid(_relicZoneBrowseScreen)
                && !ReferenceEquals(_relicZoneBrowseScreen, simple))
            {
                DismissRelicZoneSimpleScreen(_relicZoneBrowseScreen);
            }

            _relicZoneBrowseScreen = simple;
            _pendingKind = RelicGridKind.None;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NOverlayStack), nameof(NOverlayStack.Remove))]
    private static void AfterOverlayRemove(IOverlayScreen screen)
    {
        if (_activeTrunkSideDeckScreen != null && ReferenceEquals(screen, _activeTrunkSideDeckScreen))
        {
            if (screen is NDeckCardSelectScreen deck)
                TrunkSideDeckDeckCardSelectScreenPatch.ClearActiveEditorChrome(deck);
            _activeTrunkSideDeckScreen = null;
        }

        if (_relicZoneBrowseScreen != null && ReferenceEquals(screen, _relicZoneBrowseScreen))
            _relicZoneBrowseScreen = null;
    }
}
