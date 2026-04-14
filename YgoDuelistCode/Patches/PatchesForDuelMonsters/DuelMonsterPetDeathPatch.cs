using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Godot;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
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

            try
            {
                NetCombatCard nc = NetCombatCard.FromModel(card);
                GD.Print(
                    $"[YgoDuelist][MP][DuelDeath] card={card.Id?.Entry} combatCardIdx={nc.CombatCardIndex} ownerNet={player.NetId} petCombatId={pet.CombatId}");
            }
            catch (Exception ex)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP][DuelDeath] NetCombatCard missing for source card={card.Id?.Entry} ownerNet={player.NetId} petCombatId={pet.CombatId}: {ex.Message}");
            }

            if (card is Burning_Algae)
            {
                var cs = pet.CombatState;
                if (cs != null)
                {
                    TaskHelper.RunSafely(HealAllEnemiesAsync(cs, 10m));
                }
            }

            if (card is BaseMonsterCard bmc && bmc.DuelMonsterRace == DuelMonsterRace.Dragon)
                GraveyardRelic.RegisterDragonMonsterDestroyed(player);

            if (card is BaseMonsterCard level8Plus && level8Plus.DuelMonsterLevel >= 8 && level8Plus is not Berserk_Dragon)
                YgoDealWithDarkRulerState.RegisterLevel8PlusMonsterSentToGraveyard(player);

            if (card is BaseMonsterCard fairySrc
                && fairySrc.DuelMonsterRace == DuelMonsterRace.Fairy
                && YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<The_Sanctuary_in_the_Sky>(player))
            {
                bool dieForYou = pet.HasPower<DieForYouPower>()
                    || (MonsterCommandRegistry.TryGet(pet, out var monsterCommandState) && monsterCommandState.DieForYouEnabled);
                if (dieForYou)
                    GraveyardRelic.ArmSanctuaryHalveNextSpillDamage(player);
            }

            // If the current option pile is for this monster, clear it so the player can't use options pointing at a dead monster.
            var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
            if (optionPile != null && optionPile.Cards.Count > 0)
            {
                bool pileIsForThisMonster = optionPile.Cards.Any(c =>
                    c is MonsterCommandCard mcc && MonsterCommandCardMatchesDeadFieldMonster(mcc, card, pet));
                if (TributeSummonGridSelect.VerboseMpLog
                    && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
                {
                    uint deadId = pet.CombatId ?? 0;
                    GD.Print(
                        $"[YgoDuelist][MP][OptionPileDeath] ownerNet={player.NetId} deadPetCombatId={deadId} deadCard={card.Id?.Entry} pileCount={optionPile.Cards.Count} pileForDeadMonster={pileIsForThisMonster}");
                    foreach (CardModel opt in optionPile.Cards)
                    {
                        if (opt is MonsterCommandCard mcc)
                        {
                            GD.Print(
                                $"[YgoDuelist][MP][OptionPileDeath]   mcc srcPetId={mcc.SourcePetCombatId} srcMonsterNull={mcc.SourceMonster == null} matches={MonsterCommandCardMatchesDeadFieldMonster(mcc, card, pet)}");
                        }
                    }
                }

                if (pileIsForThisMonster)
                {
                    YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.MonsterOptions);
                    optionPile.Clear();
                    YgoOptionHandBridge.SyncFromOptionPile(player);
                    GD.Print("[ZGO] DuelMonsterPetDeathPatch: cleared option pile (was for dead monster).");
                }
            }

            if (card is Amazoness_Swords_Woman aws && player.Creature != null)
                TaskHelper.RunSafely(AmazonessSwordsWomanThornsSync.StripBeforeFieldUnregisterAsync(aws, player.Creature));

            if (card is Zone_Eater)
            {
                var cs = pet.CombatState;
                if (cs != null)
                    TaskHelper.RunSafely(ZoneEaterMarkPower.RemoveAllFromSourceCardAsync(cs, card));
            }

            if (card is Yomi_Ship yomi
                && MonsterCommandRegistry.TryGet(pet, out var cmdState)
                && cmdState.DestroyedByEnemyBattleDamage)
                TaskHelper.RunSafely(Yomi_Ship.ApplyBlightWhenDestroyedByBattleAsync(player, yomi));

            if (card is Electric_Lizard electricLizard
                && MonsterCommandRegistry.TryGet(pet, out var cmdElectric)
                && cmdElectric.DestroyedByEnemyBattleDamage
                && cmdElectric.BattleDamageKillerEnemy != null)
                TaskHelper.RunSafely(Electric_Lizard.ApplyWhenDestroyedByBattleAsync(player, electricLizard, cmdElectric.BattleDamageKillerEnemy));

            if (card is Rigorous_Reaver rigorous
                && MonsterCommandRegistry.TryGet(pet, out var cmdRigorous)
                && cmdRigorous.DestroyedByEnemyBattleDamage)
                TaskHelper.RunSafely(Rigorous_Reaver.ApplyWhenDestroyedByBattleAsync(player, rigorous));

            if (card is Poisonous_Snake_Token
                && MonsterCommandRegistry.TryGet(pet, out var cmdSnake)
                && cmdSnake.DestroyedByEnemyBattleDamage)
                TaskHelper.RunSafely(Poisonous_Snake_Token.ApplyWhenDestroyedByBattleAsync(player, cmdSnake.BattleDamageKillerEnemy));

            if (card is Mother_Grizzly motherGrizzly
                && MonsterCommandRegistry.TryGet(pet, out var cmdMother)
                && cmdMother.DestroyedByEnemyBattleDamage)
                YgoMotherGrizzlyBattleDeathGate.Mark(motherGrizzly);

            if (card is Pyramid_Turtle pyramidTurtle
                && MonsterCommandRegistry.TryGet(pet, out var cmdPyramid)
                && cmdPyramid.DestroyedByEnemyBattleDamage)
                YgoPyramidTurtleBattleDeathGate.Mark(pyramidTurtle);

            if (card is Mystic_Tomato mysticTomato
                && MonsterCommandRegistry.TryGet(pet, out var cmdMysticTomato)
                && cmdMysticTomato.DestroyedByEnemyBattleDamage)
                YgoMysticTomatoBattleDeathGate.Mark(mysticTomato);

            if (card is Shining_Angel shiningAngel
                && MonsterCommandRegistry.TryGet(pet, out var cmdShiningAngel)
                && cmdShiningAngel.DestroyedByEnemyBattleDamage)
                YgoShiningAngelBattleDeathGate.Mark(shiningAngel);

            if (card is Giant_Germ giantGerm
                && MonsterCommandRegistry.TryGet(pet, out var cmdGiantGerm)
                && cmdGiantGerm.DestroyedByEnemyBattleDamage)
                YgoGiantGermBattleDeathGate.Mark(giantGerm);

            if (card is Lord_Poison lordPoison
                && MonsterCommandRegistry.TryGet(pet, out var cmdLordPoison)
                && cmdLordPoison.DestroyedByEnemyBattleDamage)
                YgoLordPoisonBattleDeathGate.Mark(lordPoison);

            if (player.Creature?.HasPower<AccumulatedSpiritsPower>() == true)
                YgoDuelistPassivePowerState.RegisterAccumulatedSpiritsFieldLoss(player);

            // MP: fire-and-forget async could finish after PlayCardAction / checksum; keep block+heal before pile moves.
            RunRelocationBlocking(async () => await GuardianSpiritPower.OnPlayerDuelMonsterDestroyedAsync(
                new BlockingPlayerChoiceContext(),
                player,
                pet));

            var graveyard = CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
            bool bounceToHand = YgoDuelMonsterBounceToHand.TryConsume(pet);

            if (bounceToHand)
            {
                CardPile? hand = PileType.Hand.GetPile(player);
                if (hand != null && graveyard != null && card.Pile != hand)
                {
                    GD.Print($"[ZGO] DuelMonsterPetDeathPatch: bounce {card.Id.Entry} to hand (equips to GY).");
                    RunRelocationBlocking(() => MoveEquipsToGraveyardThenMonsterToPileAsync(player, card, hand, graveyard));
                }
            }
            else if (graveyard != null && card.Pile != graveyard)
            {
                if (card is Keldo keldo)
                {
                    GD.Print($"[ZGO] DuelMonsterPetDeathPatch: Keldo {card.Id.Entry} — move to GY then graveyard→discard effect.");
                    RunRelocationBlocking(() => Keldo.RunAfterDestroyedOnFieldAsync(player, keldo, graveyard));
                }
                else
                {
                    GD.Print($"[ZGO] DuelMonsterPetDeathPatch: moving {card.Id.Entry} (and equips) toward Graveyard.");
                    RunRelocationBlocking(() => MoveEquipsToGraveyardThenMonsterToPileAsync(player, card, graveyard, graveyard));
                }
            }

            // Remove from field/command registries so it no longer affects stats or menus.
            DuelMonsterFieldRegistry.UnregisterPet(pet);
            MonsterCommandRegistry.Clear(pet);
            if (card is Enraged_Battle_Ox)
                TaskHelper.RunSafely(EnragedBattleOxService.SyncPlayerPowerAsync(player));
            if (player?.Creature != null)
                RunRelocationBlocking(() => FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player));
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

    /// <summary>
    /// Card pile moves must finish before <see cref="DuelMonsterFieldRegistry.UnregisterPet"/> / RemoveCreature.
    /// Fire-and-forget <see cref="TaskHelper.RunSafely"/> let those run first; <see cref="CardPileCmd.Add"/> could then
    /// leave the source card in no pile (vanished from GY/hand/deck UI).
    /// </summary>
    /// <summary>
    /// MP: <see cref="MonsterCommandCard.SourceMonster"/> is not replicated; host may have a reference match while client does not.
    /// Use <see cref="MonsterCommandCard.SourcePetCombatId"/> vs the dying pet's <see cref="Creature.CombatId"/>, and resolve source when possible.
    /// </summary>
    private static bool MonsterCommandCardMatchesDeadFieldMonster(MonsterCommandCard mcc, CardModel deadFieldCard, Creature pet)
    {
        _ = mcc.TryResolveSourceMonsterFromStoredPetId();
        if (mcc.SourceMonster != null && ReferenceEquals(mcc.SourceMonster, deadFieldCard))
            return true;
        uint pid = pet.CombatId ?? 0;
        return pid != 0 && mcc.SourcePetCombatId == pid;
    }

    private static void RunRelocationBlocking(Func<Task> work)
    {
        try
        {
            work().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"DuelMonsterPetDeathPatch relocation failed: {ex}");
        }
    }

    private static async Task HealAllEnemiesAsync(CombatState cs, decimal amount)
    {
        foreach (Creature enemy in cs.Enemies.Where(e => e.IsAlive))
            await CreatureCmd.Heal(enemy, amount);
    }

    internal static async Task MoveEquipsToGraveyardThenMonsterToPileAsync(
        Player player,
        BaseMonsterCard card,
        CardPile monsterDestination,
        CardPile graveyardForEquips)
    {
        IReadOnlyList<BaseEquipSpellCard> equips = YgoEquipSpellRegistry.TakeAllEquipsFromMonster(card);
        foreach (BaseEquipSpellCard eq in equips)
        {
            if (eq.Pile?.Type == SpellTrapZonePile.CustomType)
            {
                await CardPileCmd.Add(
                    new CardModel[] { eq },
                    graveyardForEquips,
                    CardPilePosition.Top,
                    eq,
                    false);
            }
        }

        IReadOnlyList<CardModel> linkTraps = YgoSpellTrapEquipLinkRegistry.TakeAllLinksFromMonster(card);
        foreach (CardModel trap in linkTraps)
        {
            if (trap.Pile?.Type == SpellTrapZonePile.CustomType)
            {
                await CardPileCmd.Add(
                    new CardModel[] { trap },
                    graveyardForEquips,
                    CardPilePosition.Top,
                    trap,
                    false);
            }
        }

        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);

        if (card.Pile != monsterDestination)
        {
            await CardPileCmd.Add(
                new CardModel[] { card },
                monsterDestination,
                CardPilePosition.Top,
                card,
                false);
        }
    }
}
