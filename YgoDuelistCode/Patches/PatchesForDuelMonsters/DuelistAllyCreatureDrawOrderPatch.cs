using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;
using ByrdpipMonster = MegaCrit.Sts2.Core.Models.Monsters.Byrdpip;
using YgoDuelistCharacter = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// After vanilla <see cref="NCombatRoom"/> layout, sibling order draws later children on top.
/// Synchronous postfixes only: deferred reordering can diverge across peers in multiplayer.
/// Allies and enemies live in separate containers, so duel monsters can still be hidden under enemies when the
/// enemy container is later. Put allies after enemies at the container level, then order duel monsters inside allies.
/// Vanilla puts the local player at index 0 and pets after — so pets cover the player. For The Duelist, we move
/// <see cref="DuelMonsterModel"/> pets above other allies, the local player above those duel monsters, and the
/// local player's Byrdpip pet above the local player, without touching <see cref="CanvasItem.ZIndex"/>.
/// </summary>
public static class DuelistAllyCreatureDrawOrder
{
    private static string? _lastDebugSignature;
    private static CanvasItem? _activeHoveredEnemyGui;
    private static int _hoverEnemyGuiSerial;
    private static readonly Dictionary<CanvasItem, HoverEnemyGuiRestoreState> HoverEnemyGuiRestoreStates = new();

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

        Node? ally = room.GetNodeOrNull<Node>("%AllyContainer");
        if (ally == null || playerNode.GetParent() != ally)
        {
            PrintDebugOnce("skip-no-ally",
                $"ally={DescribeNode(ally)} player={DescribeNode(playerNode)} playerParent={DescribeNode(playerNode.GetParent())}");
            return;
        }

        int count = ally.GetChildCount();
        if (count < 2)
        {
            PrintDebugOnce("skip-ally-child-count", $"ally={DescribeNode(ally)} childCount={count}");
            return;
        }

        var children = new List<Node>(count);
        for (int i = 0; i < count; i++)
        {
            Node ch = ally.GetChild(i);
            if (GodotObject.IsInstanceValid(ch))
                children.Add(ch);
        }

        var duelNodes = new List<Node>();
        var ownByrdpipNodes = new List<Node>();
        foreach (Node n in children)
        {
            if (n is NCreature nc
                && nc.Entity.PetOwner == me
                && nc.Entity.Monster is DuelMonsterModel)
            {
                duelNodes.Add(n);
            }

            if (n is NCreature byrdpip
                && byrdpip.Entity.PetOwner == me
                && byrdpip.Entity.Monster is ByrdpipMonster)
            {
                ownByrdpipNodes.Add(n);
            }
        }

        if (duelNodes.Count == 0 && ownByrdpipNodes.Count == 0)
        {
            PrintDebugOnce("skip-no-duel-pets-or-byrdpip", $"ally={DescribeNode(ally)} children={DescribeChildren(ally)}");
            return;
        }

        MoveAllyContainerAboveEnemyContainer(room, ally);

        var duelSet = new HashSet<Node>(duelNodes);
        var ownByrdpipSet = new HashSet<Node>(ownByrdpipNodes);
        var others = new List<Node>();
        foreach (Node n in children)
        {
            if (n == playerNode)
                continue;
            if (duelSet.Contains(n))
                continue;
            if (ownByrdpipSet.Contains(n))
                continue;
            others.Add(n);
        }

        var desired = new List<Node>(count);
        desired.AddRange(others);
        desired.AddRange(duelNodes);
        desired.Add(playerNode);
        desired.AddRange(ownByrdpipNodes);

        if (desired.Count != children.Count)
            return;

        for (int i = 0; i < desired.Count; i++)
            ally.MoveChild(desired[i], i);
    }

    public static void RefreshLayoutAfterDuelPetRosterChanged(string reason)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room))
            return;

        CombatState? combat = CombatManager.Instance?.DebugOnlyGetState();

        ICombatRoomVisuals? visuals = Traverse.Create(room).Field<ICombatRoomVisuals>("_visuals").Value;
        float scaling = visuals?.Encounter.GetCameraScaling() ?? 1f;
        bool fullyCenterPlayers = visuals?.Encounter.FullyCenterPlayers ?? false;

        List<NCreature> allies = room.CreatureNodes
            .Where(n => n != null
                && GodotObject.IsInstanceValid(n)
                && (n.Entity.IsPlayer || n.Entity.PetOwner != null))
            .ToList();

        if (allies.Count == 0)
            return;

        NCombatRoom.PositionPlayersAndPets(allies, scaling, fullyCenterPlayers);
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(combat?.Players))
        {
            if (player.Character is YgoDuelistCharacter)
                DuelMonsterVisualLayout.ApplyForOwner(player);
        }

        Apply();
        GD.Print($"[YgoDuelist][DrawOrder] refreshed ally pet layout reason={reason} allies={allies.Count}");
    }

    public static void OnCreatureFocused(NCreature creature)
    {
        if (!IsLocalDuelistCombat())
            return;

        if (!IsEnemyCreature(creature))
        {
            ScheduleRestoreActiveHoveredEnemyGui();
            return;
        }

        CanvasItem? gui = GetCreatureStateDisplay(creature);
        if (gui == null)
            return;

        if (_activeHoveredEnemyGui != null && _activeHoveredEnemyGui != gui)
            ScheduleRestoreHoveredEnemyGui(_activeHoveredEnemyGui);

        RaiseHoveredEnemyGui(gui);
    }

    public static void OnCreatureUnfocused(NCreature creature)
    {
        if (!IsLocalDuelistCombat() || !IsEnemyCreature(creature))
            return;

        CanvasItem? gui = GetCreatureStateDisplay(creature);
        if (gui != null && gui == _activeHoveredEnemyGui)
            ScheduleRestoreHoveredEnemyGui(gui);
    }

    private static void MoveAllyContainerAboveEnemyContainer(NCombatRoom room, Node ally)
    {
        Node? enemy = room.GetNodeOrNull<Node>("%EnemyContainer");
        if (enemy == null)
        {
            PrintDebugOnce("skip-no-enemy-container", $"ally={DescribeNode(ally)}");
            return;
        }

        Node? parent = FindCommonParentBranchParent(ally, enemy, out Node allyBranch, out Node enemyBranch);
        if (parent == null)
        {
            PrintDebugOnce("skip-no-common-parent", $"ally={DescribeNode(ally)} enemy={DescribeNode(enemy)}");
            return;
        }

        int beforeAllyIndex = allyBranch.GetIndex();
        int beforeEnemyIndex = enemyBranch.GetIndex();
        if (allyBranch.GetIndex() < enemyBranch.GetIndex())
            parent.MoveChild(allyBranch, enemyBranch.GetIndex());

        PrintDebugOnce("applied",
            $"ally={DescribeNode(ally)} enemy={DescribeNode(enemy)} parent={DescribeNode(parent)} " +
            $"allyBranch={DescribeNode(allyBranch)} enemyBranch={DescribeNode(enemyBranch)} " +
            $"before={beforeAllyIndex}/{beforeEnemyIndex} after={allyBranch.GetIndex()}/{enemyBranch.GetIndex()}");
    }

    private static Node? FindCommonParentBranchParent(Node ally, Node enemy, out Node allyBranch, out Node enemyBranch)
    {
        allyBranch = ally;
        enemyBranch = enemy;

        var allyAncestors = new HashSet<Node>();
        for (Node? n = ally; n != null; n = n.GetParent())
            allyAncestors.Add(n);

        Node? common = null;
        for (Node? n = enemy; n != null; n = n.GetParent())
        {
            if (!allyAncestors.Contains(n))
                continue;

            common = n;
            break;
        }

        if (common == null || common == ally || common == enemy)
            return null;

        allyBranch = GetBranchUnderCommonParent(ally, common);
        enemyBranch = GetBranchUnderCommonParent(enemy, common);
        return allyBranch == enemyBranch ? null : common;
    }

    private static Node GetBranchUnderCommonParent(Node node, Node common)
    {
        Node branch = node;
        for (Node? parent = branch.GetParent(); parent != null && parent != common; parent = branch.GetParent())
            branch = parent;
        return branch;
    }

    private static bool IsLocalDuelistCombat()
    {
        CombatState? combat = CombatManager.Instance?.DebugOnlyGetState();
        if (combat == null)
            return false;

        Player? me = LocalContext.GetMe(combat.Players);
        return me?.Character is YgoDuelistCharacter;
    }

    private static bool IsEnemyCreature(NCreature creature)
    {
        return GodotObject.IsInstanceValid(creature)
            && creature.Entity.IsMonster
            && creature.Entity.PetOwner == null
            && creature.Entity.Player == null;
    }

    private static CanvasItem? GetCreatureStateDisplay(NCreature creature)
    {
        return GodotObject.IsInstanceValid(creature)
            ? creature.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar")
            : null;
    }

    private static void RaiseHoveredEnemyGui(CanvasItem gui)
    {
        if (!GodotObject.IsInstanceValid(gui))
            return;

        if (!HoverEnemyGuiRestoreStates.TryGetValue(gui, out HoverEnemyGuiRestoreState? state) || state == null)
        {
            state = new HoverEnemyGuiRestoreState(gui.ZIndex, gui.ZAsRelative);
            HoverEnemyGuiRestoreStates[gui] = state;
        }

        state.Serial = ++_hoverEnemyGuiSerial;
        _activeHoveredEnemyGui = gui;
        gui.ZAsRelative = false;
        gui.ZIndex = Mathf.Max(1, state.ZIndex + 1);
    }

    private static void ScheduleRestoreActiveHoveredEnemyGui()
    {
        if (_activeHoveredEnemyGui != null)
            ScheduleRestoreHoveredEnemyGui(_activeHoveredEnemyGui);
    }

    private static void ScheduleRestoreHoveredEnemyGui(CanvasItem gui)
    {
        if (!GodotObject.IsInstanceValid(gui))
            return;

        if (!HoverEnemyGuiRestoreStates.TryGetValue(gui, out HoverEnemyGuiRestoreState? state) || state == null)
            return;

        state.Serial = ++_hoverEnemyGuiSerial;
        if (_activeHoveredEnemyGui == gui)
            _activeHoveredEnemyGui = null;

        TaskHelper.RunSafely(RestoreHoveredEnemyGuiAfterDelayAsync(gui, state.Serial));
    }

    private static async Task RestoreHoveredEnemyGuiAfterDelayAsync(CanvasItem gui, int serial)
    {
        if (!GodotObject.IsInstanceValid(gui))
            return;

        SceneTreeTimer timer = gui.GetTree().CreateTimer(0.2);
        await gui.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);

        if (!GodotObject.IsInstanceValid(gui))
            return;

        if (!HoverEnemyGuiRestoreStates.TryGetValue(gui, out HoverEnemyGuiRestoreState? state) || state == null || state.Serial != serial)
            return;

        gui.ZIndex = state.ZIndex;
        gui.ZAsRelative = state.ZAsRelative;
        HoverEnemyGuiRestoreStates.Remove(gui);
    }

    private static void PrintDebugOnce(string reason, string details)
    {
        string signature = $"{reason}:{details}";
        if (_lastDebugSignature == signature)
            return;

        _lastDebugSignature = signature;
        GD.Print($"[YgoDuelist][DrawOrder] {reason}: {details}");
    }

    private static string DescribeNode(Node? node)
    {
        if (node == null)
            return "<null>";

        return $"{node.GetType().Name}('{node.Name}' idx={node.GetIndex()} path={node.GetPath()})";
    }

    private static string DescribeChildren(Node node)
    {
        var parts = new List<string>();
        for (int i = 0; i < node.GetChildCount(); i++)
        {
            Node child = node.GetChild(i);
            string label = child.Name.ToString();
            if (child is NCreature nc)
                label += $":{nc.Entity?.Monster?.GetType().Name}";
            parts.Add($"{i}:{label}");
        }

        return string.Join(", ", parts);
    }

    private sealed class HoverEnemyGuiRestoreState
    {
        public HoverEnemyGuiRestoreState(int zIndex, bool zAsRelative)
        {
            ZIndex = zIndex;
            ZAsRelative = zAsRelative;
        }

        public int ZIndex { get; }
        public bool ZAsRelative { get; }
        public int Serial { get; set; }
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

[HarmonyPatch(typeof(NCreature), "OnFocus")]
public static class DuelistHoveredEnemyGuiDrawOrderFocusPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCreature __instance)
    {
        DuelistAllyCreatureDrawOrder.OnCreatureFocused(__instance);
    }
}

[HarmonyPatch(typeof(NCreature), "OnUnfocus")]
public static class DuelistHoveredEnemyGuiDrawOrderUnfocusPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCreature __instance)
    {
        DuelistAllyCreatureDrawOrder.OnCreatureUnfocused(__instance);
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
