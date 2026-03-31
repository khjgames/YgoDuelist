using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Purple highlight on Fusion Gate in the Spell/Trap zone when a Fusion Summon is currently legal.
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "UpdateCard")]
public static class YgoFusionGateFieldGlowPatch
{
    private static readonly Color FusionGatePurple = new(0.78f, 0.42f, 1f, 0.98f);

    static void Postfix(NHandCardHolder __instance)
    {
        if (!CombatManager.Instance.IsInProgress || !CombatManager.Instance.IsPlayPhase)
            return;

        CardModel? model = __instance.CardNode?.Model;
        if (model is not Fusion_Gate gate || gate.FaceDown)
            return;
        if (gate.Pile?.Type != SpellTrapZonePile.CustomType)
            return;

        Player? owner = gate.Owner;
        if (owner == null)
            return;

        try
        {
            if (!LocalContext.IsMe(owner))
                return;
        }
        catch
        {
            return;
        }

        if (!FusionSummonSelection.HasFeasibleFusionPlay(owner, gate))
            return;

        NCard? node = __instance.CardNode;
        if (node == null)
            return;

        node.CardHighlight.AnimShow();
        node.CardHighlight.Modulate = FusionGatePurple;
    }
}
