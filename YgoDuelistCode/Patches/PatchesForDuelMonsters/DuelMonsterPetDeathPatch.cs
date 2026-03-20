using System;
using System.Linq;
using HarmonyLib;
using Godot;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Models;
using MegaCrit.Sts2.Core.Helpers;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When a summoned duel monster dies (e.g. from Die for You tanking),
/// move its source card from the MonsterPile to the GraveyardPile and
/// clear its field/command state so the zone is freed.
/// </summary>
[HarmonyPatch(typeof(PlayerCombatState), "OnPetDied")]
public static class DuelMonsterPetDeathPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerCombatState __instance, Creature pet)
    {
        try
        {
            if (pet == null || pet.Monster is not DuelMonsterModel)
                return;

            GD.Print($"[ZGO] DuelMonsterPetDeathPatch: pet died: {pet.Name}");

            var player = pet.PetOwner;
            if (player?.PlayerCombatState == null)
            {
                GD.Print("[ZGO] DuelMonsterPetDeathPatch: no pet owner/player combat state.");
                return;
            }

            var card = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (card == null)
            {
                GD.Print("[ZGO] DuelMonsterPetDeathPatch: no source card found for pet.");
                return;
            }

            TributeMaterialMarkTracker.RemoveCardFromMarkList(player, card);

            // If the current option pile is for this monster, clear it so the player can't use options pointing at a dead monster.
            var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
            if (optionPile != null && optionPile.Cards.Count > 0)
            {
                bool pileIsForThisMonster = optionPile.Cards.Any(c => c is MonsterCommandCard mcc && mcc.SourceMonster == card);
                if (pileIsForThisMonster)
                {
                    optionPile.Clear();
                    YgoOptionHandBridge.SyncFromOptionPile(player);
                    GD.Print("[ZGO] DuelMonsterPetDeathPatch: cleared option pile (was for dead monster).");
                }
            }

            // Move the monster card to the graveyard pile if possible.
            var graveyard = CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
            if (graveyard != null && card.Pile != graveyard)
            {
                GD.Print($"[ZGO] DuelMonsterPetDeathPatch: moving {card.Id.Entry} from {card.Pile?.Type} to Graveyard.");
                TaskHelper.RunSafely(CardPileCmd.Add(
                    new CardModel[] { card },
                    graveyard,
                    CardPilePosition.Top,
                    card,
                    false));
            }

            // Remove from field/command registries so it no longer affects stats or menus.
            DuelMonsterFieldRegistry.UnregisterPet(pet);
            MonsterCommandRegistry.Clear(pet);
            GD.Print("[ZGO] DuelMonsterPetDeathPatch: unregistered pet and cleared command state.");

            // Manually remove the dead duel monster's visuals and creature from combat,
            // since DieForYouPower normally prevents removal of its owner.
            var combatState = pet.CombatState;
            if (combatState != null)
            {
                var nCreature = NCombatRoom.Instance?.GetCreatureNode(pet);
                if (nCreature != null)
                {
                    GD.Print("[ZGO] DuelMonsterPetDeathPatch: removing NCreature node for dead duel monster.");
                    // Hide and free the visual immediately so the slot looks empty.
                    nCreature.Visible = false;
                    nCreature.Hitbox.Visible = false;
                    nCreature.Visuals.Bounds.Visible = false;
                    NCombatRoom.Instance.RemoveCreatureNode(nCreature);
                    nCreature.QueueFree();
                }

                if (combatState.Enemies.Contains(pet) || combatState.PlayerCreatures.Contains(pet))
                {
                    GD.Print("[ZGO] DuelMonsterPetDeathPatch: removing dead duel monster from CombatManager/CombatState.");
                    CombatManager.Instance.RemoveCreature(pet);
                    combatState.RemoveCreature(pet);
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"DuelMonsterPetDeathPatch error: {e}");
        }
    }
}
