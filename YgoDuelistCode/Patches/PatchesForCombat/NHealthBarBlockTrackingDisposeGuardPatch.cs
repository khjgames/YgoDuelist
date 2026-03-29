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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCreatureStateDisplay), "OnBlockTrackingCreatureBlockChanged")]
    public static bool OnBlockTrackingCreatureBlockChanged_GuardDisposed(NCreatureStateDisplay __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance))
            return false;

        var bar = FStateDisplayHealthBar.GetValue(__instance) as NHealthBar;
        if (bar == null || !GodotObject.IsInstanceValid(bar))
            return false;

        return NHealthBarNodesAlive(bar);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NHealthBar), "RefreshBlockUi")]
    public static bool RefreshBlockUi_GuardDisposed(NHealthBar __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance))
            return false;

        return NHealthBarNodesAlive(__instance);
    }

    private static bool NHealthBarNodesAlive(NHealthBar bar)
    {
        foreach (FieldInfo f in new[] { FHealthBarBlockContainer, FHealthBarBlockOutline, FHealthBarHpForeground })
        {
            var node = f.GetValue(bar) as GodotObject;
            if (node == null || !GodotObject.IsInstanceValid(node))
                return false;
        }

        return true;
    }
}
