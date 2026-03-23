using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Tracks <see cref="NSimpleCardSelectScreen"/> instances opened from Graveyard / Extra Deck relics so the same relic click can dismiss them.
/// </summary>
public static class YgoRelicBrowseGridOverlayPatch
{
    public enum RelicGridKind
    {
        None,
        Graveyard,
        ExtraDeck
    }

    private static RelicGridKind _pendingKind;
    private static RelicGridKind _activeKind;
    private static NSimpleCardSelectScreen? _activeScreen;

    private static readonly MethodInfo? CompleteSelection =
        AccessTools.Method(typeof(NSimpleCardSelectScreen), "CompleteSelection");

    public static void SetPendingKind(RelicGridKind kind) => _pendingKind = kind;

    public static void ClearPendingKind() => _pendingKind = RelicGridKind.None;

    /// <summary>If this browse grid is already open for the same relic type, close it (empty selection) and return true.</summary>
    public static bool TryToggleClose(RelicGridKind relicKind)
    {
        if (_activeKind != relicKind || _activeScreen == null || !GodotObject.IsInstanceValid(_activeScreen))
            return false;
        CompleteSelection?.Invoke(_activeScreen, null);
        return true;
    }

    /// <summary>Opening a different relic browse screen stacks on the overlay; dismiss any active browse grid first.</summary>
    public static void CloseAnyActiveBrowseGrid()
    {
        if (_activeScreen == null || !GodotObject.IsInstanceValid(_activeScreen))
            return;
        CompleteSelection?.Invoke(_activeScreen, null);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NOverlayStack), nameof(NOverlayStack.Push))]
    private static void AfterOverlayPush(IOverlayScreen screen)
    {
        if (_pendingKind == RelicGridKind.None)
            return;
        if (screen is NSimpleCardSelectScreen simple)
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
        if (_activeScreen != null && ReferenceEquals(screen, _activeScreen))
        {
            _activeScreen = null;
            _activeKind = RelicGridKind.None;
        }
    }
}
