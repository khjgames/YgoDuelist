using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
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

        DuelMonsterPortraitDecorations.RefreshPet(pet);
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
