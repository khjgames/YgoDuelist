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
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
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

            var card = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
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

            AbstractMonsterCard? abstractMonster = card as AbstractMonsterCard;
            MonsterCommandState? cmdState = MonsterCommandRegistry.TryGet(pet, out MonsterCommandState st) ? st : null;

            // 🔥 Fix: Die For You fallback → treat as battle kill
            if (cmdState != null
                && !cmdState.DestroyedByEnemyBattleDamage
                && cmdState.DieForYouRedirectedBattleDamageDealer is Creature dealer
                && dealer.Side == CombatSide.Enemy)
            {
                cmdState.DestroyedByEnemyBattleDamage = true;
                cmdState.BattleDamageKillerEnemy = dealer;

                GD.Print(
                    $"[YgoDuelist][BattleDeath] DieForYou fallback battle kill pet={pet.Name} killer={dealer.Name}");
            }

            var ctx = new DuelMonsterPetDeathContext(player, pet, cmdState);

            if (abstractMonster != null)
                TaskHelper.RunSafely(abstractMonster.OnPetDiedBeforeOptionPileHandlingAsync(ctx));

            TryClearOptionPileForFieldMonster(player, card, pet);

            if (abstractMonster != null)
                TaskHelper.RunSafely(abstractMonster.OnPetDiedAfterOptionPileHandlingAsync(ctx));

            if (player.Creature?.HasPower<AccumulatedSpiritsPower>() == true)
                YgoDuelistPassivePowerState.RegisterAccumulatedSpiritsFieldLoss(player);

            // GuardianSpirit / CardPileCmd use Cmd.Wait + SceneTreeTimer + tweens. Sync .GetResult() on the main
            // thread while still inside OnPetDied (invoked from CreatureCmd.Kill) deadlocks: timers never tick
            // (e.g. Special Summon XYZ kills first material → freeze). Run the whole tail on the next idle tick.
            ScheduleDeferredPetDeathRelocationChain(() => RunDeferredPetDeathTailAsync(player, pet, card, ctx));
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"DuelMonsterPetDeathPatch error: {e}");
        }
    }

    private static async Task NotifyZoneCardsAfterDuelMonsterDiedAsync(Player player, DuelMonsterPetDeathContext ctx)
    {
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return;

        foreach (CardModel c in zone.Cards.ToList())
        {
            if (c is IYgoAfterDuelMonsterDiedZoneCard hook)
                await hook.AfterDuelMonsterDiedAsync(ctx);
        }
    }

    private static void ScheduleDeferredPetDeathRelocationChain(Func<Task> tailAsync)
    {
        try
        {
            if (Engine.GetMainLoop() is SceneTree tree && tree.Root != null)
            {
                GD.Print("[YgoDuelist][MP][DuelDeath] scheduling deferred pet-death relocation tail (SceneTreeTimer / tween safe)");
                // Callable return values are marshaled to Variant; Task is not supported — fire-and-forget via void body.
                Callable.From(() => { TaskHelper.RunSafely(tailAsync()); }).CallDeferred();
                return;
            }

            GD.PrintErr("[YgoDuelist][MP][DuelDeath] SceneTree missing; running relocation tail inline (timer deadlock risk).");
            RunRelocationBlockingSync(tailAsync);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"ScheduleDeferredPetDeathRelocationChain: {ex}");
        }
    }

    private static async Task RunDeferredPetDeathTailAsync(Player player, Creature pet, CardModel card, DuelMonsterPetDeathContext ctx)
    {
        GD.Print(
            $"[YgoDuelist][MP][DuelDeath] deferred tail BEGIN pet={pet.Name} card={card.Id?.Entry} ownerNet={player.NetId} petCombatId={pet.CombatId}");

        await GuardianSpiritPower.OnPlayerDuelMonsterDestroyedAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            pet);

        CardPile? graveyard = CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
        bool bounceToHand = YgoDuelMonsterBounceToHand.TryConsume(pet);

        if (bounceToHand)
        {
            CardPile? hand = YgoPlayerPiles.Hand(player);
            if (hand != null && graveyard != null && card.Pile != hand && card is BaseMonsterCard bmBounce)
            {
                GD.Print($"[ZGO] DuelMonsterPetDeathPatch: bounce {card.Id?.Entry} to hand (equips to GY).");
                await MoveEquipsToGraveyardThenMonsterToPileAsync(player, bmBounce, hand, graveyard);
            }
        }
        else if (graveyard != null && card.Pile != graveyard)
        {
            if (card is IYgoCustomFieldMonsterDeathGraveyardRelocation customGy)
            {
                GD.Print($"[ZGO] DuelMonsterPetDeathPatch: custom GY relocation {card.Id?.Entry}");
                await customGy.RunCustomFieldMonsterDeathGraveyardRelocationAsync(player, graveyard);
            }
            else if (card is BaseMonsterCard bmToGy)
            {
                GD.Print($"[ZGO] DuelMonsterPetDeathPatch: moving {card.Id?.Entry} (and equips) toward Graveyard.");
                await MoveEquipsToGraveyardThenMonsterToPileAsync(player, bmToGy, graveyard, graveyard);
            }
        }

        DuelMonsterFieldRegistry.UnregisterPet(pet);
        MonsterCommandRegistry.Clear(pet);

        await NotifyZoneCardsAfterDuelMonsterDiedAsync(player, ctx);

        if (card is BaseMonsterCard bmDeathHook)
            await TaskHelper.RunSafely(bmDeathHook.OnAfterDuelMonsterPetDeathBeforeUnregisterAsync(player));
        if (player.Creature != null)
            await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);
        GD.Print("[ZGO] DuelMonsterPetDeathPatch: unregistered pet and cleared command state.");

        CombatState? combatState = pet.CombatState;
        if (combatState != null)
        {
            RemoveDuelMonsterNodeIfPresent(pet, "death-tail");

            // Duel pets are ally monsters with PetOwner set and Player == null, so they are not in PlayerCreatures.
            if (combatState.ContainsCreature(pet))
            {
                GD.Print("[ZGO] DuelMonsterPetDeathPatch: removing dead duel monster from CombatManager/CombatState.");
                CombatManager.Instance.RemoveCreature(pet);
                combatState.RemoveCreature(pet);
            }
        }

        DuelistAllyCreatureDrawOrder.RefreshLayoutAfterDuelPetRosterChanged("death-tail");

        GD.Print(
            $"[YgoDuelist][MP][DuelDeath] deferred tail END pet={pet.Name} card={card.Id?.Entry} ownerNet={player.NetId}");
    }

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

    private static void RunRelocationBlockingSync(Func<Task> work)
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

    internal static async Task MoveEquipsToGraveyardThenMonsterToPileAsync(
        Player player,
        BaseMonsterCard card,
        CardPile monsterDestination,
        CardPile graveyardForEquips,
        CardPilePosition monsterInsertPosition = CardPilePosition.Top)
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
                monsterInsertPosition,
                card,
                false);
        }
    }

    /// <summary>
    /// Removes a live duel monster from the field by moving its source card to the draw pile (bottom). Equips and
    /// equip-link traps go to the graveyard. Does not run pet-death hooks; the pet is killed after unregister so the
    /// card is not relocated to the graveyard by <see cref="Postfix"/>.
    /// </summary>
    public static async Task ReleaseLiveFieldMonsterToDrawPileAsync(
        Player player,
        Creature pet,
        BaseMonsterCard fieldCard,
        CardPile drawPile,
        CardPile graveyardForEquips)
    {
        if (player?.PlayerCombatState == null || pet == null || fieldCard == null)
            return;
        if (!pet.IsAlive)
            return;
        if (!DuelMonsterFieldRegistry.HasSourceCard(pet, fieldCard))
            return;

        TryClearOptionPileForFieldMonster(player, fieldCard, pet);

        if (graveyardForEquips == null)
            return;

        await MoveEquipsToGraveyardThenMonsterToPileAsync(
            player,
            fieldCard,
            drawPile,
            graveyardForEquips,
            CardPilePosition.Bottom);

        DuelMonsterFieldRegistry.UnregisterPet(pet);
        MonsterCommandRegistry.Clear(pet);

        if (player.Creature != null)
            await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);

        await CreatureCmd.Kill(pet, force: true);

        RemoveDuelMonsterNodeIfPresent(pet, "release-draw");

        CombatState? combatState = pet.CombatState;
        if (combatState != null && combatState.ContainsCreature(pet))
        {
            CombatManager.Instance.RemoveCreature(pet);
            combatState.RemoveCreature(pet);
        }

        DuelistAllyCreatureDrawOrder.RefreshLayoutAfterDuelPetRosterChanged("release-draw");
    }

    /// <summary>
    /// Removes a live duel monster from the field by moving its source card to the banished pile. Equips and
    /// equip-link traps go to the graveyard. Does not run pet-death hooks or <see cref="CreatureCmd.Kill"/> (no
    /// graveyard hop for the monster card).
    /// </summary>
    public static async Task ReleaseLiveFieldMonsterToBanishedAsync(Player player, Creature pet, BaseMonsterCard fieldCard)
    {
        if (player?.PlayerCombatState == null || pet == null || fieldCard == null)
            return;
        if (!pet.IsAlive)
            return;
        if (!DuelMonsterFieldRegistry.HasSourceCard(pet, fieldCard))
            return;

        TryClearOptionPileForFieldMonster(player, fieldCard, pet);

        CardPile? banished = YgoPlayerPiles.Banished(player);
        CardPile? graveyard = CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
        if (banished == null || graveyard == null)
            return;

        await MoveEquipsToGraveyardThenMonsterToPileAsync(player, fieldCard, banished, graveyard);

        DuelMonsterFieldRegistry.UnregisterPet(pet);
        MonsterCommandRegistry.Clear(pet);

        if (player.Creature != null)
            await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);

        // Banish path: card is already off the field; kill the pet so it leaves PlayerCombatState.Pets (CountLiveDuelMonsters).
        // Unregister first so DuelMonsterPetDeathPatch does not try to move the card to the graveyard again.
        await CreatureCmd.Kill(pet, force: true);

        RemoveDuelMonsterNodeIfPresent(pet, "release-banished");

        CombatState? combatState = pet.CombatState;
        if (combatState != null && combatState.ContainsCreature(pet))
        {
            CombatManager.Instance.RemoveCreature(pet);
            combatState.RemoveCreature(pet);
        }

        DuelistAllyCreatureDrawOrder.RefreshLayoutAfterDuelPetRosterChanged("release-banished");
    }

    /// <summary>
    /// Removes a live duel monster from the field by moving its source card to the Limbo pile (Union-Effect equip material).
    /// Equips and equip-link traps go to the graveyard.
    /// </summary>
    public static async Task ReleaseLiveFieldMonsterToLimboAsync(Player player, Creature pet, BaseMonsterCard fieldCard)
    {
        if (player?.PlayerCombatState == null || pet == null || fieldCard == null)
            return;
        if (!pet.IsAlive)
            return;
        if (!DuelMonsterFieldRegistry.HasSourceCard(pet, fieldCard))
            return;

        TryClearOptionPileForFieldMonster(player, fieldCard, pet);

        CardPile? limbo = YgoPlayerPiles.Limbo(player);
        CardPile? graveyard = CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
        if (limbo == null || graveyard == null)
            return;

        await MoveEquipsToGraveyardThenMonsterToPileAsync(player, fieldCard, limbo, graveyard);

        DuelMonsterFieldRegistry.UnregisterPet(pet);
        MonsterCommandRegistry.Clear(pet);

        if (player.Creature != null)
            await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);

        await CreatureCmd.Kill(pet, force: true);

        RemoveDuelMonsterNodeIfPresent(pet, "release-limbo");

        CombatState? combatState = pet.CombatState;
        if (combatState != null && combatState.ContainsCreature(pet))
        {
            CombatManager.Instance.RemoveCreature(pet);
            combatState.RemoveCreature(pet);
        }

        DuelistAllyCreatureDrawOrder.RefreshLayoutAfterDuelPetRosterChanged("release-limbo");
    }

    /// <summary>
    /// Removes a live duel monster from the field by moving its source card to hand. Equips and equip-link traps on that
    /// monster are sent to the graveyard (same pile moves as bounce-to-hand on death). Does not run pet-death hooks
    /// (<see cref="AbstractMonsterCard.OnPetDiedBeforeOptionPileHandlingAsync"/>, destruction powers, etc.).
    /// </summary>
    public static async Task ReleaseLiveFieldMonsterToHandAsync(Player player, Creature pet, BaseMonsterCard fieldCard)
    {
        if (player?.PlayerCombatState == null || pet == null || fieldCard == null)
            return;
        if (!pet.IsAlive)
            return;
        if (!DuelMonsterFieldRegistry.HasSourceCard(pet, fieldCard))
            return;

        TryClearOptionPileForFieldMonster(player, fieldCard, pet);

        CardPile? hand = YgoPlayerPiles.Hand(player);
        CardPile? graveyard = CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
        if (hand == null || graveyard == null)
            return;

        await MoveEquipsToGraveyardThenMonsterToPileAsync(player, fieldCard, hand, graveyard);

        DuelMonsterFieldRegistry.UnregisterPet(pet);
        MonsterCommandRegistry.Clear(pet);

        if (player.Creature != null)
            await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);

        await CreatureCmd.Kill(pet, force: true);

        RemoveDuelMonsterNodeIfPresent(pet, "release-hand");

        CombatState? combatState = pet.CombatState;
        if (combatState != null && combatState.ContainsCreature(pet))
        {
            CombatManager.Instance.RemoveCreature(pet);
            combatState.RemoveCreature(pet);
        }

        DuelistAllyCreatureDrawOrder.RefreshLayoutAfterDuelPetRosterChanged("release-hand");
    }

    private static void RemoveDuelMonsterNodeIfPresent(Creature pet, string reason)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        var nCreature = room?.GetCreatureNode(pet);
        if (room == null || nCreature == null || !GodotObject.IsInstanceValid(nCreature))
            return;

        GD.Print($"[ZGO] DuelMonsterPetDeathPatch: removing NCreature node for duel monster reason={reason}.");
        nCreature.Visible = false;
        nCreature.Hitbox.Visible = false;
        nCreature.Visuals.Bounds.Visible = false;
        room.RemoveCreatureNode(nCreature);
        nCreature.QueueFree();
    }

    private static void TryClearOptionPileForFieldMonster(Player player, CardModel fieldMonsterCard, Creature pet)
    {
        var optionPile = YgoPlayerPiles.OptionPile(player);
        if (optionPile == null || optionPile.Cards.Count == 0)
            return;

        bool pileIsForThisMonster = optionPile.Cards.Any(c =>
            c is MonsterCommandCard mcc && MonsterCommandCardMatchesDeadFieldMonster(mcc, fieldMonsterCard, pet));
        if (TributeSummonGridSelect.VerboseMpLog
            && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            uint deadId = pet.CombatId ?? 0;
            GD.Print(
                $"[YgoDuelist][MP][OptionPileDeath] ownerNet={player.NetId} deadPetCombatId={deadId} deadCard={fieldMonsterCard.Id?.Entry} pileCount={optionPile.Cards.Count} pileForDeadMonster={pileIsForThisMonster}");
            foreach (CardModel opt in optionPile.Cards)
            {
                if (opt is MonsterCommandCard mcc)
                {
                    GD.Print(
                        $"[YgoDuelist][MP][OptionPileDeath]   mcc srcPetId={mcc.SourcePetCombatId} srcMonsterNull={mcc.SourceMonster == null} matches={MonsterCommandCardMatchesDeadFieldMonster(mcc, fieldMonsterCard, pet)}");
                }
            }
        }

        if (!pileIsForThisMonster)
            return;

        YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.MonsterOptions);
        optionPile.Clear();
        YgoOptionHandBridge.SyncFromOptionPile(player);
        GD.Print("[ZGO] DuelMonsterPetDeathPatch: cleared option pile (was for dead monster).");
    }
}
