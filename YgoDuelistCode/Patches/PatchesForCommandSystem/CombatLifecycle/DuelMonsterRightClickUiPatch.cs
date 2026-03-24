using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Models;
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
            YgoEquipPortraitOverlaySync.RefreshSpellTrapRowEquipOverlays(me);
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
        var entity = __instance.Entity;
        Player? owner = entity?.PetOwner;
        if (_hoveredPet == entity)
        {
            GD.Print($"[YgoDuelist] Hover end on duel pet: {entity?.Monster?.GetType().Name}");
            _hoveredPet = null;
            YgoEquipPortraitOverlaySync.RefreshSpellTrapRowEquipOverlays(owner);
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
            DuelMonsterMonsterOptionsMenu.OpenMonsterOptions(pet);
        }
        catch (Exception e)
        {
            // Log and swallow; this patch must never crash the game.
            YgoDuelist.MainFile.Logger.Error($"DuelMonsterRightClickUiPatch error: {e}");
        }
    }
}
