using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCombat;

/// <summary>
/// Pet rows that call <see cref="NCreature.TrackBlockStatus"/> mirror the player's block on the pet health bar.
/// When that UI is torn down while the player still gains or loses block, vanilla can run
/// <see cref="NHealthBar.RefreshBlockUi"/> / <see cref="NHealthBar.RefreshValues"/> on disposed controls.
/// Also fixes a vanilla leak: <c>TrackBlockStatus</c> never unsubscribes the previous <c>_blockTrackingCreature</c>.
/// Replay drain inside <see cref="Patches.YgoReplayPatch"/> extends the same play action — block mirror updates can
/// still race pet/player health bar teardown.
/// </summary>
public static class NHealthBarBlockTrackingDisposeGuardPatch
{
    private static readonly FieldInfo FStateDisplayBlockTrack =
        AccessTools.Field(typeof(NCreatureStateDisplay), "_blockTrackingCreature");

    private static readonly MethodInfo MOnBlockTrackingChanged =
        AccessTools.Method(typeof(NCreatureStateDisplay), "OnBlockTrackingCreatureBlockChanged");

    private static readonly FieldInfo FStateDisplayHealthBar =
        AccessTools.Field(typeof(NCreatureStateDisplay), "_healthBar");

    private static readonly FieldInfo FHealthBarBlockContainer =
        AccessTools.Field(typeof(NHealthBar), "_blockContainer");

    private static readonly FieldInfo FHealthBarBlockOutline =
        AccessTools.Field(typeof(NHealthBar), "_blockOutline");

    private static readonly FieldInfo FHealthBarHpForeground =
        AccessTools.Field(typeof(NHealthBar), "_hpForeground");

    private static readonly FieldInfo FHealthBarBlockLabel =
        AccessTools.Field(typeof(NHealthBar), "_blockLabel");

    private static readonly FieldInfo FHealthBarPoisonForeground =
        AccessTools.Field(typeof(NHealthBar), "_poisonForeground");

    private static readonly FieldInfo FHealthBarDoomForeground =
        AccessTools.Field(typeof(NHealthBar), "_doomForeground");

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCreatureStateDisplay), nameof(NCreatureStateDisplay.TrackBlockStatus))]
    public static void TrackBlockStatus_UnsubscribePrevious(NCreatureStateDisplay __instance)
    {
        var oldCreature = FStateDisplayBlockTrack.GetValue(__instance) as Creature;
        if (oldCreature == null)
            return;

        var handler = (Action<int, int>)Delegate.CreateDelegate(typeof(Action<int, int>), __instance, MOnBlockTrackingChanged);
        oldCreature.BlockChanged -= handler;
    }

    internal static NHealthBar? GetHealthBar(NCreatureStateDisplay display)
    {
        if (!GodotObject.IsInstanceValid(display) || !display.IsInsideTree())
            return null;

        var bar = FStateDisplayHealthBar.GetValue(display) as NHealthBar;
        return HealthBarUiFullySafe(bar) ? bar : null;
    }

    internal static bool HealthBarUiFullySafe(NHealthBar? bar)
    {
        if (bar == null || !GodotObject.IsInstanceValid(bar) || !bar.IsInsideTree())
            return false;

        foreach (FieldInfo f in new[]
                 {
                     FHealthBarBlockContainer,
                     FHealthBarBlockOutline,
                     FHealthBarHpForeground,
                     FHealthBarBlockLabel,
                     FHealthBarPoisonForeground,
                     FHealthBarDoomForeground,
                 })
        {
            if (f.GetValue(bar) is not Node node || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree())
                return false;
        }

        return true;
    }

    internal static bool AllowOriginalOrSkip(NHealthBar __instance) =>
        HealthBarUiFullySafe(__instance);

    internal static Exception? SwallowDisposedControl(Exception? __exception) =>
        __exception is ObjectDisposedException ? null : __exception;
}

[HarmonyPatch]
public static class NCreatureStateDisplayBlockTrackingRefreshPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(NCreatureStateDisplay), "OnBlockTrackingCreatureBlockChanged")!;

    [HarmonyPrefix]
    public static bool OnBlockTrackingCreatureBlockChanged_SafeRefresh(NCreatureStateDisplay __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree())
            return false;

        var bar = NHealthBarBlockTrackingDisposeGuardPatch.GetHealthBar(__instance);
        if (bar == null)
            return false;

        try
        {
            bar.RefreshValues();
        }
        catch (ObjectDisposedException)
        {
        }

        return false;
    }
}

[HarmonyPatch]
public static class NCreatureStateDisplayAnimateInBlockSafePatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(NCreatureStateDisplay), "AnimateInBlock")!;

    [HarmonyPrefix]
    public static bool AnimateInBlock_Safe(NCreatureStateDisplay __instance, int oldBlock, int blockGain)
    {
        if (oldBlock != 0 || blockGain == 0)
            return false;

        if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree())
            return false;

        var bar = NHealthBarBlockTrackingDisposeGuardPatch.GetHealthBar(__instance);
        if (bar == null)
            return false;

        try
        {
            bar.AnimateInBlock(oldBlock, blockGain);
        }
        catch (ObjectDisposedException)
        {
        }

        return false;
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshBlockUi")]
public static class NHealthBarRefreshBlockUiDisposeGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NHealthBar __instance) =>
        NHealthBarBlockTrackingDisposeGuardPatch.AllowOriginalOrSkip(__instance);

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception) =>
        NHealthBarBlockTrackingDisposeGuardPatch.SwallowDisposedControl(__exception);
}

[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
public static class NHealthBarRefreshForegroundDisposeGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NHealthBar __instance) =>
        NHealthBarBlockTrackingDisposeGuardPatch.AllowOriginalOrSkip(__instance);

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception) =>
        NHealthBarBlockTrackingDisposeGuardPatch.SwallowDisposedControl(__exception);
}

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.RefreshValues))]
public static class NHealthBarRefreshValuesDisposeGuardPatch
{
    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception) =>
        NHealthBarBlockTrackingDisposeGuardPatch.SwallowDisposedControl(__exception);
}

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.AnimateInBlock))]
public static class NHealthBarAnimateInBlockDisposeGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NHealthBar __instance) =>
        NHealthBarBlockTrackingDisposeGuardPatch.AllowOriginalOrSkip(__instance);

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception) =>
        NHealthBarBlockTrackingDisposeGuardPatch.SwallowDisposedControl(__exception);
}
