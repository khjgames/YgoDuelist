using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// NTargetManager._Process calls FinishTargeting(cancel: true) when _exitEarlyCondition() is true.
/// For option-row plays that condition (e.g. IsCardInCancelZone()) can be true immediately, so
/// targeting is cancelled before the arrow appears. Skip the exit-condition cancel entirely when
/// the current card play is from an option holder so the user can see the arrow and click an enemy.
/// </summary>
[HarmonyPatch(typeof(NTargetManager), "_Process")]
public static class NTargetManagerProcessOptionHolderPatch
{
    private static readonly FieldInfo ExitEarlyConditionField =
        AccessTools.Field(typeof(NTargetManager), "_exitEarlyCondition");

    private static readonly PropertyInfo HoveredNodeProp =
        AccessTools.Property(typeof(NTargetManager), "HoveredNode");

    private static readonly FieldInfo TargetModeField =
        AccessTools.Field(typeof(NTargetManager), "_targetMode");

    private static readonly FieldInfo CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    private static readonly MethodInfo FinishTargetingMethod =
        AccessTools.Method(typeof(NTargetManager), "FinishTargeting");

    static bool Prefix(NTargetManager __instance, double delta)
    {
        if (!__instance.IsInSelection)
            return true;

        var exitEarly = ExitEarlyConditionField?.GetValue(__instance) as System.Func<bool>;
        if (exitEarly != null && exitEarly())
        {
            var hand = NPlayerHand.Instance;
            if (hand != null && hand.InCardPlay && CurrentCardPlayField?.GetValue(hand) is NCardPlay cp && cp.Holder is NYgoOptionCardHolder)
            {
                // Option holder: do not cancel from exit condition; let user click target or right-click to cancel.
            }
            else
            {
                FinishTargetingMethod?.Invoke(__instance, new object[] { true });
            }
        }

        var hovered = HoveredNodeProp?.GetValue(__instance);
        if (hovered is NCreature nCreature)
        {
            var entity = nCreature.Entity;
            var mode = TargetModeField?.GetValue(__instance);
            if (entity != null && !entity.IsHittable && mode != null && (TargetMode)mode == TargetMode.Controller)
            {
                FinishTargetingMethod?.Invoke(__instance, new object[] { true });
            }
        }

        return false;
    }
}
