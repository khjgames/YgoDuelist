using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Diagnostic: log when TryPlayCard is called for an option holder so we can see if targeting
/// completed and with what target (null = cancel, non-null = enqueue action).
/// </summary>
[HarmonyPatch(typeof(NCardPlay), "TryPlayCard")]
public static class NCardPlayTryPlayCardOptionLogPatch
{
    static void Prefix(NCardPlay __instance, Creature? target)
    {
        if (__instance.Holder is not NYgoOptionCardHolder)
            return;
        GD.Print("[YgoDuelist] TryPlayCard ENTER option holder TargetType=", __instance.Holder.CardNode?.Model?.TargetType.ToString() ?? "null", " target=", target != null ? "CREATURE" : "NULL");
    }
}
