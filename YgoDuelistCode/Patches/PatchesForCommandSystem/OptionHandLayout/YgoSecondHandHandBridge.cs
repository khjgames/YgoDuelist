using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Helper to register NYgoOptionCardHolder instances with the real NPlayerHand,
/// using its private AddCardHolder so all signals and layout wiring are identical
/// to normal hand cards.
/// </summary>
public static class YgoSecondHandHandBridge
{
    private static readonly MethodInfo AddCardHolderMethod =
        AccessTools.Method(typeof(NPlayerHand), "AddCardHolder", new[] { typeof(NHandCardHolder), typeof(int) });

    public static void RegisterOptionHolder(NPlayerHand hand, NYgoOptionCardHolder holder, int index)
    {
        GD.Print("[YgoDuelist] RegisterOptionHolder ENTER handId=", hand.GetInstanceId(), " holderId=", holder.GetInstanceId(), " index=", index, " handChildCountBefore=", hand.GetChildCount());
        AddCardHolderMethod.Invoke(hand, new object[] { holder, index });
        GD.Print("[YgoDuelist] RegisterOptionHolder EXIT handId=", hand.GetInstanceId(), " holderId=", holder.GetInstanceId(), " handChildCountAfter=", hand.GetChildCount(), " holderIsInsideTree=", holder.IsInsideTree());
    }
}
