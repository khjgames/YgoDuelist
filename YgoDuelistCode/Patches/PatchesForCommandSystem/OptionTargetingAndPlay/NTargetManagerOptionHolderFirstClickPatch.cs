using Godot;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When the user starts playing a targeting card from the option row by clicking (not drag),
/// the same click's release would immediately finish targeting with no hovered enemy, so
/// the red arrow never effectively appears and the play gets no target. Skip finishing
/// targeting when we would complete with cancel=false and HoveredNode=null and the current
/// card play is from an option holder, so the user can then click an enemy to select it.
/// </summary>
[HarmonyPatch(typeof(NTargetManager), "FinishTargeting")]
public static class NTargetManagerOptionHolderFirstClickPatch
{
    private static readonly PropertyInfo HoveredNodeProp =
        AccessTools.Property(typeof(NTargetManager), "HoveredNode");

    private static readonly FieldInfo CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    private static long _optionHolderTargetingStartFrame;

    public static void RecordOptionHolderTargetingStart(NTargetManager targetManager)
    {
        if (targetManager?.GetTree() != null)
            _optionHolderTargetingStartFrame = targetManager.GetTree().GetFrame();
    }

    static bool Prefix(NTargetManager __instance, bool cancel)
    {
        if (cancel)
        {
            // Don't cancel in the first ~30 frames so the arrow can appear (_Process exit condition can be true immediately).
            var hand = NPlayerHand.Instance;
            if (hand != null && hand.InCardPlay && CurrentCardPlayField?.GetValue(hand) is NCardPlay cp && cp.Holder is NYgoOptionCardHolder)
            {
                long now = __instance.GetTree().GetFrame();
                if (now - _optionHolderTargetingStartFrame < 30 && _optionHolderTargetingStartFrame > 0)
                    return false;
            }
            _optionHolderTargetingStartFrame = 0;
            return true;
        }

        if (HoveredNodeProp?.GetValue(__instance) != null)
        {
            _optionHolderTargetingStartFrame = 0;
            return true;
        }

        var hand2 = NPlayerHand.Instance;
        if (hand2 == null || !hand2.InCardPlay)
        {
            _optionHolderTargetingStartFrame = 0;
            return true;
        }

        var cardPlay = CurrentCardPlayField?.GetValue(hand2);
        if (cardPlay is not NCardPlay play)
        {
            _optionHolderTargetingStartFrame = 0;
            return true;
        }

        if (play.Holder is not NYgoOptionCardHolder)
        {
            _optionHolderTargetingStartFrame = 0;
            return true;
        }

        // Would finish with no target while playing from option row: keep targeting active.
        GD.Print("[YgoDuelist] FinishTargeting: blocking (no hovered target, option holder) - wait for enemy click or right-click to cancel");
        return false;
    }
}
