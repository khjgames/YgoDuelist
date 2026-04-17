using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelistCharacter = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// After vanilla <see cref="NCombatRoom"/> layout, sibling order under <c>%AllyContainer</c> draws later children on top.
/// Synchronous postfixes only: deferred reordering can diverge across peers in multiplayer.
/// Vanilla puts the local player at index 0 and pets after — so pets cover the player. For The Duelist, we move
/// <see cref="DuelMonsterModel"/> pets to sit just under the local player node (second-to-last among that group)
/// and move the local player last so they draw on top, without touching <see cref="CanvasItem.ZIndex"/>.
/// </summary>
public static class DuelistAllyCreatureDrawOrder
{
    public static void Apply()
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (room == null)
            return;

        CombatState? combat = CombatManager.Instance?.DebugOnlyGetState();
        if (combat == null)
            return;

        Player? me = LocalContext.GetMe(combat.Players);
        if (me?.Character is not YgoDuelistCharacter)
            return;

        NCreature? playerNode = room.GetCreatureNode(me.Creature);
        if (playerNode == null || !GodotObject.IsInstanceValid(playerNode))
            return;

        Control? ally = room.GetNodeOrNull<Control>("%AllyContainer");
        if (ally == null || playerNode.GetParent() != ally)
            return;

        int count = ally.GetChildCount();
        if (count < 2)
            return;

        var children = new List<Node>(count);
        for (int i = 0; i < count; i++)
        {
            Node ch = ally.GetChild(i);
            if (GodotObject.IsInstanceValid(ch))
                children.Add(ch);
        }

        var duelNodes = new List<Node>();
        foreach (Node n in children)
        {
            if (n is NCreature nc
                && nc.Entity.PetOwner == me
                && nc.Entity.Monster is DuelMonsterModel)
            {
                duelNodes.Add(n);
            }
        }

        if (duelNodes.Count == 0)
            return;

        var duelSet = new HashSet<Node>(duelNodes);
        var others = new List<Node>();
        foreach (Node n in children)
        {
            if (n == playerNode)
                continue;
            if (duelSet.Contains(n))
                continue;
            others.Add(n);
        }

        var desired = new List<Node>(count);
        desired.AddRange(others);
        desired.AddRange(duelNodes);
        desired.Add(playerNode);

        if (desired.Count != children.Count)
            return;

        for (int i = 0; i < desired.Count; i++)
            ally.MoveChild(desired[i], i);
    }
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public static class DuelistAllyCreatureDrawOrderAfterLayoutPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix()
    {
        DuelistAllyCreatureDrawOrder.Apply();
    }
}

/// <summary>Also runs when <see cref="DuelMonsterScalePatch"/> returns early (non–duel-monster pets).</summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class DuelistAllyCreatureDrawOrderAfterAddCreaturePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix()
    {
        DuelistAllyCreatureDrawOrder.Apply();
    }
}
