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
/// Tracks relic browse grids (<see cref="NSimpleCardSelectScreen"/> and trunk/side <see cref="NDeckCardSelectScreen"/>)
/// so the same relic click can dismiss them.
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

    private static RelicGridKind _pendingKind;
    private static RelicGridKind _activeKind;
    private static NSimpleCardSelectScreen? _activeScreen;
    private static NDeckCardSelectScreen? _activeTrunkSideDeckScreen;

    private static readonly MethodInfo? CompleteSelection =
        AccessTools.Method(typeof(NSimpleCardSelectScreen), "CompleteSelection", Type.EmptyTypes);

    private static readonly FieldInfo? DeckSelectedCardsField =
        AccessTools.Field(typeof(NDeckCardSelectScreen), "_selectedCards");

    private static readonly FieldInfo? CompletionSourceField =
        AccessTools.Field(typeof(NCardGridSelectionScreen), "_completionSource");

    public static void SetPendingKind(RelicGridKind kind) => _pendingKind = kind;

    public static void ClearPendingKind() => _pendingKind = RelicGridKind.None;

    /// <summary>
    /// Binds the trunk/side editor screen for nav dismissal. Called from chrome injection so it is set even if <see cref="NOverlayStack.Push"/> postfix
    /// has not run yet relative to <see cref="NDeckCardSelectScreen._Ready"/>.
    /// </summary>
    public static void RegisterActiveTrunkSideDeckScreen(NDeckCardSelectScreen deck)
    {
        if (deck == null || !GodotObject.IsInstanceValid(deck))
            return;
        _activeKind = RelicGridKind.TrunkSideDeckSelect;
        _activeTrunkSideDeckScreen = deck;
    }

    /// <summary>Closes the trunk/side grid with an empty selection and queues opening <paramref name="targetPage"/> on the next editor loop iteration.</summary>
    public static void CompleteActiveTrunkSideNavigate(TrunkSideDeckEditorPage targetPage)
    {
        if (_activeKind != RelicGridKind.TrunkSideDeckSelect || _activeTrunkSideDeckScreen == null || !GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
            return;
        TrunkSideDeckEditorSession.RequestNavigateTo(targetPage);
        if (_activeTrunkSideDeckScreen != null && GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
            ClearTrunkSideDeckScreenEmpty(_activeTrunkSideDeckScreen);
    }

    /// <summary>If this browse grid is already open for the same relic type, close it and return true.</summary>
    public static bool TryToggleClose(RelicGridKind relicKind)
    {
        if (_activeKind != relicKind)
            return false;

        if (relicKind == RelicGridKind.TrunkSideDeckSelect)
        {
            if (_activeTrunkSideDeckScreen == null || !GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
                return false;
            TrunkSideDeckEditorSession.ClearNavigateRequest();
            ClearTrunkSideDeckScreenEmpty(_activeTrunkSideDeckScreen);
            return true;
        }

        if (_activeScreen == null || !GodotObject.IsInstanceValid(_activeScreen))
            return false;

        CompleteSelection?.Invoke(_activeScreen, null);
        return true;
    }

    /// <summary>Opening a different relic browse screen stacks on the overlay; dismiss any active browse grid first.</summary>
    public static void CloseAnyActiveBrowseGrid()
    {
        if (_activeTrunkSideDeckScreen != null && GodotObject.IsInstanceValid(_activeTrunkSideDeckScreen))
        {
            TrunkSideDeckEditorSession.ClearNavigateRequest();
            ClearTrunkSideDeckScreenEmpty(_activeTrunkSideDeckScreen);
            return;
        }

        if (_activeScreen == null || !GodotObject.IsInstanceValid(_activeScreen))
            return;

        CompleteSelection?.Invoke(_activeScreen, null);
    }

    private static void ClearTrunkSideDeckScreenEmpty(NDeckCardSelectScreen screen)
    {
        if (DeckSelectedCardsField?.GetValue(screen) is ISet<CardModel> set)
            set.Clear();
        if (CompletionSourceField?.GetValue(screen) is TaskCompletionSource<IEnumerable<CardModel>> tcs)
            tcs.SetResult(Array.Empty<CardModel>());
        NOverlayStack.Instance.Remove(screen);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NOverlayStack), nameof(NOverlayStack.Push))]
    private static void AfterOverlayPush(IOverlayScreen screen)
    {
        if (_pendingKind == RelicGridKind.None)
            return;

        if (_pendingKind == RelicGridKind.TrunkSideDeckSelect && screen is NDeckCardSelectScreen deck)
        {
            _activeKind = RelicGridKind.TrunkSideDeckSelect;
            _activeTrunkSideDeckScreen = deck;
        }
        else if (screen is NSimpleCardSelectScreen simple)
        {
            _activeKind = _pendingKind;
            _activeScreen = simple;
        }

        _pendingKind = RelicGridKind.None;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NOverlayStack), nameof(NOverlayStack.Remove))]
    private static void AfterOverlayRemove(IOverlayScreen screen)
    {
        if (_activeTrunkSideDeckScreen != null && ReferenceEquals(screen, _activeTrunkSideDeckScreen))
        {
            _activeTrunkSideDeckScreen = null;
            if (_activeKind == RelicGridKind.TrunkSideDeckSelect)
                _activeKind = RelicGridKind.None;
        }

        if (_activeScreen != null && ReferenceEquals(screen, _activeScreen))
        {
            _activeScreen = null;
            _activeKind = RelicGridKind.None;
        }
    }
}
