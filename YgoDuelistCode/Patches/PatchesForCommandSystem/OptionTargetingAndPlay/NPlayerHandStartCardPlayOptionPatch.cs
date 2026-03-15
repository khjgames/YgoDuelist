using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When starting a card play from the option row (second hand), skip the "drag into play zone"
/// requirement so targeting (red arrow) starts immediately. Option cards are already in a
/// dedicated row; forcing the player to drag them up into the main play zone prevents
/// targeting from ever appearing.
/// </summary>
[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
public static class NPlayerHandStartCardPlayOptionPatch
{
    static void Prefix(NHandCardHolder holder, ref bool startedViaShortcut)
    {
        if (holder is NYgoOptionCardHolder)
        {
            startedViaShortcut = true;
            Godot.GD.Print("[YgoDuelist] StartCardPlay PREFIX: option holder - set startedViaShortcut=true");
        }
    }

    static void Postfix(NHandCardHolder holder)
    {
        if (holder is NYgoOptionCardHolder)
            Godot.GD.Print("[YgoDuelist] StartCardPlay POSTFIX: option holder - NMouseCardPlay created and Start() called");
    }
}
