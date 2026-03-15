using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// For option-row plays the mouse is often in the "cancel zone" (bottom 5% of screen),
/// so the exit condition (IsCardInCancelZone()) is true immediately. That makes both
/// _Process and _Input call FinishTargeting(cancel: true) before the user sees the arrow.
/// Replace the exit condition with a no-op for option holders so targeting stays active
/// until the user clicks an enemy or right-clicks to cancel.
/// </summary>
[HarmonyPatch(typeof(NTargetManager), "StartTargeting", new[] { typeof(TargetType), typeof(Vector2), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>) })]
[HarmonyPatch(typeof(NTargetManager), "StartTargeting", new[] { typeof(TargetType), typeof(Control), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>) })]
public static class NTargetManagerStartTargetingOptionHolderPatch
{
    private static readonly FieldInfo ExitEarlyConditionField =
        AccessTools.Field(typeof(NTargetManager), "_exitEarlyCondition");

    private static readonly FieldInfo CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    private static readonly System.Func<bool> NoOpExitCondition = () => false;

    static void Prefix(NTargetManager __instance, TargetType validTargetsType, object __1)
    {
        GD.Print("[YgoDuelist] StartTargeting ENTER validTargetsType=", validTargetsType, " secondArgType=", __1?.GetType().Name ?? "null");
    }

    static void Postfix(NTargetManager __instance)
    {
        var hand = NPlayerHand.Instance;
        if (hand == null)
            return;

        var cardPlay = CurrentCardPlayField?.GetValue(hand);
        var isOptionHolder = cardPlay is NCardPlay cp && cp.Holder is NYgoOptionCardHolder;
        GD.Print("[YgoDuelist] StartTargeting POSTFIX cardPlay=", cardPlay?.GetType().Name ?? "null", " holderIsOption=", isOptionHolder);

        if (!isOptionHolder)
            return;

        GD.Print("[YgoDuelist] StartTargeting: option holder play - disabling exit-early condition so targeting/arrow stay active");
        ExitEarlyConditionField?.SetValue(__instance, NoOpExitCondition);
    }
}
