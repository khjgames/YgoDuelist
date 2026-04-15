using System.Threading.Tasks;
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
    /// <summary>Queue sync after card stance/face-down changed (e.g. keywords updated). No-op if card has no field summon.</summary>
    public static void RequestSyncIfSummoned(AbstractMonsterCard card)
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        TaskHelper.RunSafely(SyncIfSummonedAsync(card));
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

        await PowerCmd.Remove<AttackPositionPower>(pet);
        await PowerCmd.Remove<DefensePositionPower>(pet);
        await PowerCmd.Remove<FaceDownStancePower>(pet);

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
    /// <see cref="RequestSyncIfSummoned"/> completing after the snapshot). Uses <see cref="PowerModel.RemoveInternal"/> /
    /// <see cref="PowerModel.ApplyInternal"/> like <see cref="MonsterCommandRegistry.ApplyDieForYouSyncForChecksum"/>.
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
        RemovePowerIfPresentSyncForChecksum<AttackPositionPower>(pet);
        RemovePowerIfPresentSyncForChecksum<DefensePositionPower>(pet);
        RemovePowerIfPresentSyncForChecksum<FaceDownStancePower>(pet);
    }

    private static void RemovePowerIfPresentSyncForChecksum<T>(Creature pet) where T : PowerModel
    {
        T? power = pet.GetPower<T>();
        if (power != null)
            power.RemoveInternal();
    }

    private static void ApplyPowerSyncForChecksum<T>(Creature pet, Creature applier) where T : PowerModel
    {
        PowerModel proto = ModelDb.Power<T>();
        PowerModel power = proto.ToMutable();
        power.Applier = applier;
        power.ApplyInternal(pet, 1m, silent: true);
    }

    private static Creature? FindLivePetForCard(Player owner, BaseMonsterCard card)
    {
        if (owner.PlayerCombatState == null)
            return null;

        foreach (Creature pet in owner.PlayerCombatState.Pets)
        {
            if (pet.IsAlive && DuelMonsterFieldRegistry.GetSourceCardForPet(pet) == card)
                return pet;
        }

        return null;
    }
}
