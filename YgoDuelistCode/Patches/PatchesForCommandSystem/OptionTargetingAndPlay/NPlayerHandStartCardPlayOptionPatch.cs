using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Fixes StartCardPlay for cases where holder index is outside the vanilla 0..9 shortcut array
/// (e.g. >10 cards in hand, or option-row holders appended after normal hand cards).
/// For those holders we run a safe custom StartCardPlay path and use a fixed cancel shortcut.
/// </summary>
[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
public static class NPlayerHandStartCardPlayOptionPatch
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
        GD.Print("[YgoLifecycle] StartCardPlay ", point,
            " holderId=", holder.GetInstanceId(),
            " model=", holder.CardModel?.GetType().Name ?? "null",
            " index=", holder.GetIndex(),
            " inTree=", holder.IsInsideTree(),
            " extra=", extra);
    }

    private static readonly FieldInfo DraggedHolderIndexField =
        AccessTools.Field(typeof(NPlayerHand), "_draggedHolderIndex");

    private static readonly FieldInfo HoldersAwaitingQueueField =
        AccessTools.Field(typeof(NPlayerHand), "_holdersAwaitingQueue");

    private static readonly FieldInfo CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    private static readonly MethodInfo ReturnHolderToHandMethod =
        AccessTools.DeclaredMethod(typeof(NPlayerHand), "ReturnHolderToHand");

    private static readonly MethodInfo RefreshLayoutMethod =
        AccessTools.DeclaredMethod(typeof(NPlayerHand), "RefreshLayout");

    static bool Prefix(NPlayerHand __instance, NHandCardHolder holder, ref bool startedViaShortcut)
    {
        int originalIndex = holder.GetIndex();
        bool needsSafePath = holder is NYgoOptionCardHolder || originalIndex < 0 || originalIndex >= 10;
        LogLifecycle("S1_EnterPrefix", holder, $"needsSafePath={needsSafePath}");
        if (!needsSafePath)
            return true;

        int queuedIndex = originalIndex < 0 ? 0 : originalIndex;
        startedViaShortcut = true;
        DraggedHolderIndexField?.SetValue(__instance, queuedIndex);

        var queue = HoldersAwaitingQueueField?.GetValue(__instance) as Dictionary<NHandCardHolder, int>;
        if (queue != null)
            queue[holder] = queuedIndex;
        LogLifecycle("S2_QueuedHolder", holder, $"queuedIndex={queuedIndex}");

        holder.Reparent(__instance);
        holder.BeginDrag();
        LogLifecycle("S3_BeginDrag", holder);

        bool usingController = NControllerManager.Instance?.IsUsingController == true;
        NCardPlay cardPlay = usingController
            ? NControllerCardPlay.Create(holder)
            : NMouseCardPlay.Create(holder, MegaInput.selectCard1, true);

        CurrentCardPlayField?.SetValue(__instance, cardPlay);
        __instance.AddChildSafely(cardPlay);
        LogLifecycle("S4_CardPlayCreated", holder, $"cardPlayType={cardPlay.GetType().Name}");

        cardPlay.Connect(NCardPlay.SignalName.Finished, Callable.From<bool>(success =>
        {
            LogLifecycle("S5_FinishedSignal", holder, $"success={success}");
            RunManager.Instance.HoveredModelTracker.OnLocalCardDeselected();
            if (!success)
            {
                LogLifecycle("S6_InvokeReturnHolderToHand", holder);
                ReturnHolderToHandMethod?.Invoke(__instance, new object[] { holder });
            }
            else if (holder is NYgoOptionCardHolder)
            {
                var player = holder.CardModel?.Owner as MegaCrit.Sts2.Core.Entities.Players.Player;
                bool shouldScrap = YgoOptionHandUiPatch.ShouldScrapOptionHolderAfterPlay(holder, player);
                LogLifecycle("S6b_SuccessPathScrapDecision", holder, $"shouldScrap={shouldScrap}");
                if (shouldScrap)
                {
                    YgoOptionHandUiPatch.ForceReleaseOptionHolder(holder);
                    LogLifecycle("S6c_SuccessPathForceRelease", holder);
                }
            }

            DraggedHolderIndexField?.SetValue(__instance, -1);
            RefreshLayoutMethod?.Invoke(__instance, null);
            LogLifecycle("S7_FinishedCleanupDone", holder);
        }));

        if (holder.CardNode?.Model != null)
            RunManager.Instance.HoveredModelTracker.OnLocalCardSelected(holder.CardNode.Model);
        cardPlay.Start();
        RefreshLayoutMethod?.Invoke(__instance, null);
        holder.SetIndexLabel(queuedIndex + 1);
        LogLifecycle("S8_StartCalled", holder);

        GD.Print("[YgoDuelist] StartCardPlay PREFIX: custom safe start path holderId=", holder.GetInstanceId(), " index=", originalIndex, " isOption=", (holder is NYgoOptionCardHolder));
        return false;
    }

    static void Postfix(NHandCardHolder holder)
    {
        if (holder is NYgoOptionCardHolder)
            Godot.GD.Print("[YgoDuelist] StartCardPlay POSTFIX: option holder - NMouseCardPlay created and Start() called");
    }
}
