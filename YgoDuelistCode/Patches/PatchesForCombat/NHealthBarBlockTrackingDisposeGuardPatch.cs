using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Pet rows that call <see cref="NCreature.TrackBlockStatus"/> mirror the player's block on the pet health bar.
/// When that UI is torn down while the player still gains or loses block, vanilla can run
/// <see cref="NHealthBar.RefreshBlockUi"/> on disposed <see cref="Control"/>s. Also fixes a vanilla leak:
/// <c>TrackBlockStatus</c> never unsubscribes the previous <c>_blockTrackingCreature</c>.
/// </summary>
public static class NHealthBarBlockTrackingDisposeGuardPatch
{
    private static readonly FieldInfo FStateDisplayBlockTrack =
        AccessTools.Field(typeof(NCreatureStateDisplay), "_blockTrackingCreature");

    private static readonly FieldInfo FStateDisplayHealthBar =
        AccessTools.Field(typeof(NCreatureStateDisplay), "_healthBar");

    private static readonly MethodInfo MOnBlockTrackingChanged =
        AccessTools.Method(typeof(NCreatureStateDisplay), "OnBlockTrackingCreatureBlockChanged");

    private static readonly FieldInfo FHealthBarBlockContainer =
        AccessTools.Field(typeof(NHealthBar), "_blockContainer");

    private static readonly FieldInfo FHealthBarBlockOutline =
        AccessTools.Field(typeof(NHealthBar), "_blockOutline");

    private static readonly FieldInfo FHealthBarHpForeground =
        AccessTools.Field(typeof(NHealthBar), "_hpForeground");

    private static readonly FieldInfo FHealthBarBlockLabel =
        AccessTools.Field(typeof(NHealthBar), "_blockLabel");

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

    /// <summary>
    /// Vanilla calls <see cref="NHealthBar.RefreshValues"/> here. In MP, <see cref="MegaCrit.Sts2.Core.GameActions.PlayCardAction"/>
    /// can yield while net messages rebuild the option row; <see cref="Control._ExitTree"/> may not have run yet, so
    /// <see cref="NHealthBar.RefreshBlockUi"/> can touch freed <see cref="Godot.NinePatchRect"/> nodes. We mirror the one-line
    /// body with tree/validity checks and swallow <see cref="ObjectDisposedException"/> only.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCreatureStateDisplay), "OnBlockTrackingCreatureBlockChanged")]
    public static bool OnBlockTrackingCreatureBlockChanged_SafeRefresh(NCreatureStateDisplay __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree())
            return false;

        var bar = FStateDisplayHealthBar.GetValue(__instance) as NHealthBar;
        if (bar == null || !GodotObject.IsInstanceValid(bar) || !bar.IsInsideTree())
            return false;

        if (!NHealthBarNodesAlive(bar))
            return false;

        try
        {
            bar.RefreshValues();
        }
        catch (ObjectDisposedException)
        {
            // UI torn down while BlockChanged still fired (e.g. OpenMonsterOptions interleaved with mirrored play).
        }

        return false;
    }

    /// <summary>
    /// Same race as <see cref="OnBlockTrackingCreatureBlockChanged_SafeRefresh"/>: <see cref="NCreatureStateDisplay.AnimateInBlock"/>
    /// forwards to <see cref="NHealthBar.AnimateInBlock"/> which sets block container visibility.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCreatureStateDisplay), "AnimateInBlock")]
    public static bool AnimateInBlock_Safe(NCreatureStateDisplay __instance, int oldBlock, int blockGain)
    {
        if (oldBlock != 0 || blockGain == 0)
            return false;

        if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree())
            return false;

        var bar = FStateDisplayHealthBar.GetValue(__instance) as NHealthBar;
        if (bar == null || !GodotObject.IsInstanceValid(bar) || !bar.IsInsideTree())
            return false;

        if (!NHealthBarNodesAlive(bar))
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NHealthBar), "RefreshBlockUi")]
    public static bool RefreshBlockUi_GuardDisposed(NHealthBar __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree())
            return false;

        return NHealthBarNodesAlive(__instance);
    }

    /// <summary>
    /// Prefix checks can still let the original run when <see cref="GodotObject.IsInstanceValid(GodotObject?)"/>
    /// disagrees with the next property read (dispose race on the same frame). Swallow only this failure mode.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(NHealthBar), "RefreshBlockUi")]
    public static Exception? RefreshBlockUi_SwallowDisposedControl(Exception? __exception)
    {
        return __exception is ObjectDisposedException ? null : __exception;
    }

    private static bool NHealthBarNodesAlive(NHealthBar bar)
    {
        foreach (FieldInfo f in new[]
                 {
                     FHealthBarBlockContainer,
                     FHealthBarBlockOutline,
                     FHealthBarHpForeground,
                     FHealthBarBlockLabel
                 })
        {
            if (f.GetValue(bar) is not Node node || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree())
                return false;
        }

        return true;
    }
}
