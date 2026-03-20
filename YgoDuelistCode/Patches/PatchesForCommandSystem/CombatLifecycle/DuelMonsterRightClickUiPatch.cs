using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

// Tracks which pet the player is currently hovering, so right-click logic
// can be driven off the same focus/hover behavior the base game uses.
[HarmonyPatch(typeof(NCreature))]
public static class DuelMonsterHoverTrackerPatch
{
    private static Creature? _hoveredPet;

    public static Creature? CurrentHoveredPet => _hoveredPet;

    [HarmonyPostfix]
    [HarmonyPatch("OnFocus")]
    private static void OnFocusPostfix(NCreature __instance)
    {
        try
        {
            var creature = __instance.Entity;
            if (creature?.PetOwner == null || creature.Monster is not DuelMonsterModel)
                return;

            var combatState = creature.CombatState;
            if (combatState == null || !CombatManager.Instance.IsInProgress)
                return;

            Player me;
            try
            {
                me = LocalContext.GetMe(combatState);
            }
            catch
            {
                return;
            }

            if (me == null || creature.PetOwner != me)
                return;

            _hoveredPet = creature;
            GD.Print($"[YgoDuelist] Hover start on duel pet: {creature.Monster?.GetType().Name}");
        }
        catch
        {
            _hoveredPet = null;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnUnfocus")]
    private static void OnUnfocusPostfix(NCreature __instance)
    {
        if (_hoveredPet == __instance.Entity)
        {
            GD.Print($"[YgoDuelist] Hover end on duel pet: {__instance.Entity.Monster?.GetType().Name}");
            _hoveredPet = null;
        }
    }
}

/// <summary>
/// Global right-click handler: if the mouse is over one of the local player's
/// DuelMonsterModel pets, open that monster's command options in YgoCardOptionPile.
/// This is patched onto NCombatUi._Input and guarded extremely defensively so
/// it never crashes the game outside of an active combat.
/// </summary>
[HarmonyPatch(typeof(NCombatUi), "_Input")]
public static class DuelMonsterRightClickUiPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance, InputEvent inputEvent)
    {
        try
        {
            //GD.Print($"[YgoDuelist] NCombatUi._Input event: {inputEvent.GetType().Name}");

            if (inputEvent is not InputEventMouseButton mouse ||
                mouse.ButtonIndex != MouseButton.Right ||
                !mouse.Pressed)
            {
                return;
            }

            GD.Print("[YgoDuelist] Right mouse button pressed.");

            // Only run when a combat is actually running.
            var combatState = CombatManager.Instance.DebugOnlyGetState();
            if (combatState == null || !CombatManager.Instance.IsInProgress)
            {
                GD.Print("[YgoDuelist] Right-click ignored: no active combat.");
                return;
            }

            // Use the same hover mechanics as the base game: if the current
            // hovered creature is one of our DuelMonster pets, treat this as
            // right-clicking that monster.
            var pet = DuelMonsterHoverTrackerPatch.CurrentHoveredPet;
            if (pet == null)
            {
                GD.Print("[YgoDuelist] Right-click: no currently hovered duel pet.");
                return;
            }

            GD.Print($"[YgoDuelist] Right-click on duel pet: {pet.Monster?.GetType().Name}");
            OpenMonsterOptions(pet);
        }
        catch (Exception e)
        {
            // Log and swallow; this patch must never crash the game.
            YgoDuelist.MainFile.Logger.Error($"DuelMonsterRightClickUiPatch error: {e}");
        }
    }

        private static void OpenMonsterOptions(Creature pet)
    {
        var player = pet.PetOwner;
        if (player?.PlayerCombatState == null)
            return;

        var sourceCard = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
        if (sourceCard is not NormalMonsterCard monsterCard)
            return;

        CardPile optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
            return;

        GD.Print($"[YgoDuelist] Clearing YgoCardOptionPile. Previous count={optionPile.Cards.Count}");
        optionPile.Clear();

        // Use CombatState.CreateCard<T> so we get proper mutable instances
        // from the canonical models registered in ModelDb, instead of
        // directly calling constructors (which throws DuplicateModelException).
        var combatState = pet.CombatState;
        if (combatState == null)
            return;

        // Order: Defend > Attack > Change position > Toggle > Exit (same as second-hand display order).
        var commands = new List<CardModel>();

        Command_Defend cmdDefend = combatState.CreateCard<Command_Defend>(player);
        cmdDefend.InitializeSource(monsterCard);
        commands.Add(cmdDefend);

        Command_Attack cmdAttack = combatState.CreateCard<Command_Attack>(player);
        cmdAttack.InitializeSource(monsterCard);
        commands.Add(cmdAttack);

        Command_Change_Battle_Position changePos = combatState.CreateCard<Command_Change_Battle_Position>(player);
        changePos.InitializeSource(monsterCard);
        commands.Add(changePos);

        if (TributeMaterialMarkTracker.ShouldShowToggleTributeSacrificeCommand(player))
        {
            Command_Toggle_Tribute_Sacrifice tributeToggle = combatState.CreateCard<Command_Toggle_Tribute_Sacrifice>(player);
            tributeToggle.InitializeSource(monsterCard);
            commands.Add(tributeToggle);
        }

        Toggle_Die_For_You toggle = combatState.CreateCard<Toggle_Die_For_You>(player);
        toggle.InitializeSource(monsterCard);
        commands.Add(toggle);

        Exit_Monster_Options exit = combatState.CreateCard<Exit_Monster_Options>(player);
        exit.InitializeSource(monsterCard);
        commands.Add(exit);

        GD.Print($"[YgoDuelist] Adding {commands.Count} command cards to YgoCardOptionPile.");
        foreach (var c in commands)
        {
            GD.Print($"[YgoDuelist]  - {c.Id.Entry} ({c.GetType().Name})");
        }

        // Move each command card into the YgoCardOptionPile using the same
        // CardPileCmd.Add overload pattern that spells use for Graveyard,
        // so BaseLib's CustomPile hooks see these cards.
        foreach (var cmd in commands.OfType<MonsterCommandCard>())
        {
            TaskHelper.RunSafely(cmd.SendThisCommandToYgoOptionPile());
        }

        // After rebuilding the underlying logical pile, sync the "second hand"
        // view so any UI subscribers (that own NCards/NCardHolders) can update.
        YgoOptionHandBridge.SyncFromOptionPile(player);
    }
}
