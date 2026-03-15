using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When the player drags a card from the option hand (second row), StartAsync can run while
/// the holder is torn down (e.g. Exit clicked during drag). Only bail out when the holder is
/// actually invalid/destroyed so we don't block the normal targeting flow for valid option holders.
/// </summary>
[HarmonyPatch(typeof(NMouseCardPlay), "StartAsync")]
public static class NMouseCardPlayStartAsyncOptionPilePatch
{
    static bool Prefix(NMouseCardPlay __instance, ref Task __result)
    {
        var holder = __instance.Holder;
        if (holder is not NYgoOptionCardHolder)
            return true;

        if (!GodotObject.IsInstanceValid(holder) || !holder.IsInsideTree())
        {
            GD.Print("[YgoDuelist] StartAsync: option holder INVALID or not in tree - cancelling play");
            __instance.CancelPlayCard();
            __result = Task.CompletedTask;
            return false;
        }

        GD.Print("[YgoDuelist] StartAsync: option holder valid - continuing to targeting (TargetType=", holder.CardNode?.Model?.TargetType.ToString() ?? "null", ")");
        return true;
    }
}
