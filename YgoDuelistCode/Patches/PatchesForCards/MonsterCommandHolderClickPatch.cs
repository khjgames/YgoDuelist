using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Previously triggered OnClickedOption for Toggle_Die_For_You and Exit_Monster_Options on left click.
/// That behavior is now in PlayCardFromOptionPilePatch: when you try to play those cards from the
/// second hand and CanPlay is false, we run OnClickedOption there.
/// </summary>
[HarmonyPatch(typeof(NCardHolder), "OnMouseReleased")]
public static class MonsterCommandHolderClickPatch
{
    public static void Postfix(NCardHolder __instance, InputEvent inputEvent)
    {
        // No longer intercept left click; play attempt from second hand is handled in
        // PlayCardFromOptionPilePatch when CanPlay is false.
    }
}
