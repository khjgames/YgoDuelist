using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When an option holder is returned to hand after a cancelled drag, the stored index
/// can be out of range (e.g. 7 when the container only has 5 children after we removed
/// other option holders). Clamp the index so MoveChild does not throw.
/// </summary>
[HarmonyPatch(typeof(NPlayerHand))]
public static class NPlayerHandReturnHolderToHandPatch
{
    private static bool IsLifecycleDebugHolder(NHandCardHolder holder)
    {
        var modelName = holder?.CardModel?.GetType().Name;
        return holder is NYgoOptionCardHolder || modelName == "Activate_Effect";
    }

    private static void LogLifecycle(string point, NHandCardHolder holder, string extra = "")
    {
        if (!IsLifecycleDebugHolder(holder))
            return;
        GD.Print("[YgoLifecycle] ReturnHolderToHand ", point,
            " holderId=", holder.GetInstanceId(),
            " model=", holder.CardModel?.GetType().Name ?? "null",
            " inTree=", holder.IsInsideTree(),
            " visible=", holder.Visible,
            " extra=", extra);
    }

    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(NPlayerHand), "ReturnHolderToHand");
    }

    static bool Prefix(NPlayerHand __instance, NHandCardHolder holder)
    {
        LogLifecycle("P1_EnterPrefix", holder);
        if (holder is not NYgoOptionCardHolder)
            return true;

        var queueField = AccessTools.Field(typeof(NPlayerHand), "_holdersAwaitingQueue");
        var queue = queueField?.GetValue(__instance) as Dictionary<NHandCardHolder, int>;

        if (!GodotObject.IsInstanceValid(holder))
        {
            LogLifecycle("P2_InvalidHolderEarlyOut", holder);
            if (queue != null)
            {
                try { queue.Remove(holder); }
                catch (System.ObjectDisposedException) { }
            }
            if (YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand == holder)
                YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand = null;
            return false;
        }

        if (!__instance.IsAwaitingPlay(holder))
        {
            LogLifecycle("P3_NotAwaitingPlay_FallbackVanilla", holder);
            return true;
        }

        if (queue == null || !queue.TryGetValue(holder, out int index))
        {
            LogLifecycle("P4_NoQueuedIndex_FallbackVanilla", holder);
            return true;
        }

        queue.Remove(holder);
        var container = __instance.CardHolderContainer;
        int count = container.GetChildCount();
        int clampedIndex = Mathf.Clamp(index, 0, count);
        LogLifecycle("P5_ReparentStart", holder, $"queued={index} clamped={clampedIndex} childCount={count}");

        holder.Reparent(container);
        if (clampedIndex >= 0)
            container.MoveChild(holder, clampedIndex);
        holder.SetDefaultTargets();
        holder.Visible = true;
        LogLifecycle("P6_ReparentDone", holder);

        var player = holder.CardModel?.Owner as Player;
        if (YgoOptionHandUiPatch.ShouldScrapOptionHolderAfterPlay(holder, player))
        {
            LogLifecycle("P7_ShouldScrap_ForceRelease", holder);
            YgoOptionHandUiPatch.ForceReleaseOptionHolder(holder);
            return false;
        }

        if (YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand == holder)
        {
            LogLifecycle("P8_PendingFreeAfterReturn", holder);
            YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand = null;
            container.RemoveChild(holder);
            holder.QueueFree();
        }
        else
        {
            // Force option row to rebuild from pile so the cancelled card is guaranteed to show.
            if (player != null)
            {
                LogLifecycle("P9_ScheduleSyncFromOptionPile", holder);
                var tree = __instance.GetTree();
                var timer = tree.CreateTimer(0.0);
                timer.Timeout += () => YgoOptionHandBridge.SyncFromOptionPile(player);
            }
        }

        LogLifecycle("P10_ExitHandled", holder);
        return false;
    }
}
