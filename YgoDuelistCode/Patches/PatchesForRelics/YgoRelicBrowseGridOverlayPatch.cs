using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Tracks exactly one zone-relic browse UI at a time (graveyard / shadow realm / extra deck) and the trunk/side deck editor.
/// Only overlays opened immediately after <see cref="SetPendingKind"/> are bound — vanilla card/potion/power grids never set pending, so they are never touched.
/// </summary>
public static class YgoRelicBrowseGridOverlayPatch
{
    public enum RelicGridKind
    {
        None,
        Graveyard,
        ShadowRealm,
        ExtraDeck,
        TrunkSideDeckSelect
    }

    /// <summary>Set in <see cref="GraveyardRelicClickPatch"/> / <see cref="TrunkSideDeckGuiService"/> right before the overlay Push; cleared when the matching screen is bound.</summary>
    private static RelicGridKind _pendingKind;

    private static RelicGridKind _relicZoneBrowseKind;
    private static NSimpleCardSelectScreen? _relicZoneBrowseScreen;

    private static NDeckCardSelectScreen? _activeTrunkSideDeckScreen;

    private static readonly FieldInfo? DeckSelectedCardsField =
        AccessTools.Field(typeof(NDeckCardSelectScreen), "_selectedCards");

    private static readonly FieldInfo? CompletionSourceField =
        AccessTools.Field(typeof(NCardGridSelectionScreen), "_completionSource");

    public static void SetPendingKind(RelicGridKind kind) => _pendingKind = kind;

    public static void ClearPendingKind() => _pendingKind = RelicGridKind.None;

    public static bool IsRelicSimpleGridOpenInFlight(RelicGridKind kind) =>
        kind is RelicGridKind.Graveyard or RelicGridKind.ShadowRealm or RelicGridKind.ExtraDeck
        && _pendingKind == kind;

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

            case RelicGridKind.Graveyard:
            case RelicGridKind.ShadowRealm:
            case RelicGridKind.ExtraDeck:
                if (_relicZoneBrowseKind != relicKind)
                    return false;
                if (_relicZoneBrowseScreen == null || !GodotObject.IsInstanceValid(_relicZoneBrowseScreen))
                {
                    _relicZoneBrowseKind = RelicGridKind.None;
                    _relicZoneBrowseScreen = null;
                    return false;
                }

                DismissRelicZoneSimpleScreen(_relicZoneBrowseScreen);
                _relicZoneBrowseKind = RelicGridKind.None;
                _relicZoneBrowseScreen = null;
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

        if (_relicZoneBrowseScreen != null && GodotObject.IsInstanceValid(_relicZoneBrowseScreen))
            DismissRelicZoneSimpleScreen(_relicZoneBrowseScreen);

        _relicZoneBrowseScreen = null;
        _relicZoneBrowseKind = RelicGridKind.None;
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
    [HarmonyPriority(Priority.First)]
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

        switch (_pendingKind)
        {
            case RelicGridKind.Graveyard:
            case RelicGridKind.ShadowRealm:
            case RelicGridKind.ExtraDeck:
                if (_relicZoneBrowseScreen != null
                    && GodotObject.IsInstanceValid(_relicZoneBrowseScreen)
                    && !ReferenceEquals(_relicZoneBrowseScreen, simple))
                {
                    DismissRelicZoneSimpleScreen(_relicZoneBrowseScreen);
                }

                _relicZoneBrowseKind = _pendingKind;
                _relicZoneBrowseScreen = simple;
                _pendingKind = RelicGridKind.None;
                break;
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
        {
            _relicZoneBrowseScreen = null;
            _relicZoneBrowseKind = RelicGridKind.None;
        }
    }
}
