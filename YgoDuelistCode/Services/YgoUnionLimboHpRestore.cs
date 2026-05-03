using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// After a Union-Effect monster returns from Limbo, sets current HP from the equip-time snapshot plus any max-HP growth since then.
/// <see cref="FortifiedBeastsDuelMonsterHp.SyncPetFromCardAsync"/> is applied first so <see cref="Creature.MaxHp"/> reflects Fortified Beasts and card bonuses;
/// then current HP becomes <c>Clamp(snapshotHp + (newMax - snapshotMaxHp), 0, newMax)</c> so the pet gains only the health that came from max increases
/// (never a full heal to snapshot max) and still keeps the snapshot fraction of HP when max is unchanged.
/// </summary>
public static class YgoUnionLimboHpRestore
{
    public static async Task ApplySnapshotAfterSummonAsync(
        Creature pet,
        BaseMonsterCard card,
        Player player,
        int snapshotHp,
        int snapshotMaxHp)
    {
        if (pet == null || !pet.IsAlive || player == null || card == null)
            return;

        await FortifiedBeastsDuelMonsterHp.SyncPetFromCardAsync(pet, card, player);

        int newMax = pet.MaxHp;
        int maxGrowthSinceSnapshot = newMax - snapshotMaxHp;
        int targetHp = snapshotHp + maxGrowthSinceSnapshot;
        if (targetHp < 0)
            targetHp = 0;
        if (targetHp > newMax)
            targetHp = newMax;

        int hpDelta = targetHp - pet.CurrentHp;
        if (hpDelta > 0)
            await CreatureCmd.Heal(pet, hpDelta, playAnim: false);
        else if (hpDelta < 0)
        {
            await CreatureCmd.Damage(
                YgoChoiceContexts.Blocking(),
                pet,
                -hpDelta,
                DamageProps.cardHpLoss,
                pet,
                card);
        }
    }
}
