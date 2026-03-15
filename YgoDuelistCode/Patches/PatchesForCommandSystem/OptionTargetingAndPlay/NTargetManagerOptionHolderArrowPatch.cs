using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When starting targeting from an option-row card, record the frame for first-click handling
/// and keep the arrow rooted at the NCard (card position is clamped via SetTargetPosition patch).
/// </summary>
[HarmonyPatch(typeof(NTargetManager), nameof(NTargetManager.StartTargeting), new[] { typeof(TargetType), typeof(Control), typeof(TargetMode), typeof(System.Func<bool>), typeof(System.Func<Godot.Node, bool>) })]
public static class NTargetManagerOptionHolderArrowPatch
{
    static void Prefix(NTargetManager __instance, Control control)
    {
        if (control == null)
            return;
        var parent = control.GetParent();
        if (parent is NYgoOptionCardHolder && GodotObject.IsInstanceValid(parent))
            NTargetManagerOptionHolderFirstClickPatch.RecordOptionHolderTargetingStart(__instance);
    }
}
