using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Keeps informational stance/face-down powers on duel monster pets in sync with their source card.
/// </summary>
public static class DuelMonsterStancePowerSync
{
    private static readonly FieldInfo?[] CreaturePowerListFields =
    {
        AccessTools.Field(typeof(Creature), "_powers"),
        AccessTools.Field(typeof(Creature), "powers"),
        AccessTools.Field(typeof(Creature), "<Powers>k__BackingField")
    };

    private static readonly FieldInfo?[] PowerOwnerFields =
    {
        AccessTools.Field(typeof(PowerModel), "_owner"),
        AccessTools.Field(typeof(PowerModel), "owner"),
        AccessTools.Field(typeof(PowerModel), "<Owner>k__BackingField")
    };

    private static bool _loggedMissingMutablePowerList;
    private static bool _loggedMissingPowerOwnerField;

    /// <summary>Queue sync after card stance/face-down changed (e.g. keywords updated). No-op if card has no field summon.</summary>
    public static void RequestSyncIfSummoned(AbstractMonsterCard card)
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        TaskHelper.RunSafely(SyncIfSummonedAsync(card));
    }

    /// <summary>
    /// Await pet stance powers after <see cref="AbstractMonsterCard.ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync"/>
    /// (replaces fire-and-forget <see cref="RequestSyncIfSummoned"/> for that path).
    /// </summary>
    public static async Task SyncSummonedPetIfPresentAsync(AbstractMonsterCard card, Creature? applier)
    {
        if (card is not BaseMonsterCard bmc || card.Owner == null)
            return;

        Creature? pet = FindLivePetForCard(card.Owner, bmc);
        if (pet == null)
            return;

        await SyncForPetAsync(pet, card, applier ?? card.Owner.Creature, card);
    }

    private static async Task SyncIfSummonedAsync(AbstractMonsterCard card)
    {
        if (card is not BaseMonsterCard bmc || card.Owner == null)
            return;

        Creature? pet = FindLivePetForCard(card.Owner, bmc);
        if (pet == null)
            return;

        await SyncForPetAsync(pet, card, card.Owner.Creature, card);
    }

    /// <summary>Apply current attack/defense/face-down powers for a pet right after summon.</summary>
    public static async Task SyncForPetAsync(Creature pet, AbstractMonsterCard card, Creature? applier, CardModel? sourceCard)
    {
        if (pet == null || !pet.IsAlive)
            return;

        Creature? app = applier ?? card.Owner?.Creature;
        CardModel? src = sourceCard ?? card;

        RemoveStancePowersInternal(pet, "gameplay-sync", preferMutableList: false);

        if (card.Type == CardType.Attack)
            await PowerCmd.Apply<AttackPositionPower>(pet, 1m, app, src);
        else
            await PowerCmd.Apply<DefensePositionPower>(pet, 1m, app, src);

        if (card.FaceDown)
            await PowerCmd.Apply<FaceDownStancePower>(pet, 1m, app, src);

        if (card is BaseMonsterCard bm)
            await bm.SyncPlayerThornsFromFieldPetPresenceAsync(pet, app, src);

        DuelMonsterPortraitDecorations.RefreshPet(pet);
    }

    /// <summary>
    /// MP checksum / <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetFullCombatState.FromRun"/> only:
    /// align stance powers on the pet with the field source card without awaiting <see cref="PowerCmd"/> (same race as
    /// <see cref="RequestSyncIfSummoned"/> completing after the snapshot). This directly edits the cosmetic power list
    /// instead of <see cref="PowerModel.ApplyInternal"/> when the internal list is available, so checksum snapshots do not
    /// instantiate <c>NPower</c> UI nodes or load icon resources while serialization is running.
    /// </summary>
    /// <remarks>
    /// Do not gate on <see cref="Creature.CanReceivePowers"/>: on another player&apos;s client, the summoner&apos;s duel pets
    /// can be false while still participating in the shared checksum (e.g. after <see cref="Cards.Command.Command_Attack"/>),
    /// which skipped stance here and caused host vs client hash mismatch on <see cref="Powers.AttackPositionPower"/>.
    /// </remarks>
    public static void ApplyStanceFromSourceCardSyncForChecksum(Creature pet, AbstractMonsterCard card, Creature? applier)
    {
        if (CombatManager.Instance?.IsEnding == true)
            return;

        Creature? app = applier ?? card.Owner?.Creature;
        if (app == null)
            return;

        RemoveStancePowersSyncForChecksum(pet);

        if (card.Type == CardType.Attack)
            ApplyPowerSyncForChecksum<AttackPositionPower>(pet, app);
        else
            ApplyPowerSyncForChecksum<DefensePositionPower>(pet, app);

        if (card.FaceDown)
            ApplyPowerSyncForChecksum<FaceDownStancePower>(pet, app);
    }

    private static void RemoveStancePowersSyncForChecksum(Creature pet)
    {
        RemoveStancePowersInternal(pet, "checksum-sync", preferMutableList: true);
    }

    private static void RemoveStancePowersInternal(Creature pet, string context, bool preferMutableList)
    {
        int attack = CountPowers<AttackPositionPower>(pet);
        int defense = CountPowers<DefensePositionPower>(pet);
        int faceDown = CountPowers<FaceDownStancePower>(pet);

        int removed =
            RemoveAllPowerCopiesInternal<AttackPositionPower>(pet, preferMutableList)
            + RemoveAllPowerCopiesInternal<DefensePositionPower>(pet, preferMutableList)
            + RemoveAllPowerCopiesInternal<FaceDownStancePower>(pet, preferMutableList);

        if (removed > 0 && (attack > 1 || defense > 1 || faceDown > 1 || (attack > 0 && defense > 0)))
        {
            GD.Print(
                $"[YgoDuelist][MP][DuelMonsterStancePowerSync] Normalized stance marker powers context={context} " +
                $"pet={pet.Monster?.Id?.Entry ?? "?"} removed={removed} before atk={attack} def={defense} faceDown={faceDown}");
        }
    }

    private static void ApplyPowerSyncForChecksum<T>(Creature pet, Creature applier) where T : PowerModel
    {
        PowerModel proto = ModelDb.Power<T>();
        PowerModel power = proto.ToMutable();
        power.Applier = applier;
        power.SetAmount(1, silent: true);

        if (TryGetMutablePowerList(pet) is { } powers && TrySetPowerOwner(power, pet))
        {
            powers.Add(power);
            return;
        }

        power.ApplyInternal(pet, 1m, silent: true);
    }

    private static IList<PowerModel>? TryGetMutablePowerList(Creature pet)
    {
        foreach (FieldInfo? field in CreaturePowerListFields)
        {
            if (field?.GetValue(pet) is IList<PowerModel> powers)
                return powers;
        }

        if (!_loggedMissingMutablePowerList)
        {
            _loggedMissingMutablePowerList = true;
            GD.PrintErr("[YgoDuelist][MP][DuelMonsterStancePowerSync] Could not resolve Creature mutable powers list; falling back to PowerModel ApplyInternal/RemoveInternal for checksum stance reconcile.");
        }

        return null;
    }

    private static bool TrySetPowerOwner(PowerModel power, Creature owner)
    {
        foreach (FieldInfo? field in PowerOwnerFields)
        {
            if (field == null)
                continue;

            try
            {
                field.SetValue(power, owner);
                return true;
            }
            catch (Exception ex)
            {
                if (!_loggedMissingPowerOwnerField)
                {
                    _loggedMissingPowerOwnerField = true;
                    GD.PrintErr($"[YgoDuelist][MP][DuelMonsterStancePowerSync] Could not set PowerModel owner through field '{field.Name}' ({ex.GetType().Name}); falling back to PowerModel ApplyInternal for checksum stance reconcile.");
                }

                return false;
            }
        }

        if (!_loggedMissingPowerOwnerField)
        {
            _loggedMissingPowerOwnerField = true;
            GD.PrintErr("[YgoDuelist][MP][DuelMonsterStancePowerSync] Could not resolve PowerModel owner field; falling back to PowerModel ApplyInternal for checksum stance reconcile.");
        }

        return false;
    }

    private static int CountPowers<T>(Creature pet) where T : PowerModel
    {
        return pet.Powers.Count(p => p is T);
    }

    private static int RemoveAllPowerCopiesInternal<T>(Creature pet, bool preferMutableList) where T : PowerModel
    {
        if (preferMutableList && TryGetMutablePowerList(pet) is { } powers)
        {
            int removed = 0;
            for (int i = powers.Count - 1; i >= 0; i--)
            {
                if (powers[i] is not T)
                    continue;

                powers.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        List<T> matches = pet.Powers.OfType<T>().ToList();
        foreach (T power in matches)
            power.RemoveInternal();
        return matches.Count;
    }

    private static Creature? FindLivePetForCard(Player owner, BaseMonsterCard card)
    {
        if (owner.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (pet.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(pet, card))
                return pet;
        }

        return null;
    }
}
