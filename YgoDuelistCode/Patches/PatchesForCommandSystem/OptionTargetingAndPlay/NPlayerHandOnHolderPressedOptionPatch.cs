using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Ensures option row holders receive the same play flow as main hand: when the user
/// clicks/drags an option card, the hand's OnHolderPressed must run so StartCardPlay
/// is invoked. This patch logs when an option holder press is received (for debugging)
/// and does not change behavior.
/// </summary>
[HarmonyPatch(typeof(NPlayerHand), "OnHolderPressed")]
public static class NPlayerHandOnHolderPressedOptionPatch
{
    static void Prefix(NCardHolder holder)
    {
        if (holder is NYgoOptionCardHolder)
            GD.Print("[YgoDuelist] OnHolderPressed option holder id=", holder.GetInstanceId(), " card=", (holder as NYgoOptionCardHolder)?.CardModel?.GetType().Name ?? "?");
    }
}
