using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// MP: stable <see cref="Creature.CombatId"/> lookups for field monsters tracked by <see cref="DuelMonsterFieldRegistry"/>.
/// Card references to <see cref="BaseMonsterCard"/> are not replicated; re-bind using the pet id after state sync.
/// </summary>
public static class YgoDuelMonsterPetBinding
{
    public static uint TryFindPetCombatIdForFieldMonster(BaseMonsterCard? monster)
    {
        if (monster?.Owner?.PlayerCombatState == null)
            return 0;
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(monster.Owner.PlayerCombatState))
        {
            if (p.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.HasSourceCard(p, monster))
                return p.CombatId ?? 0;
        }

        return 0;
    }

    public static BaseMonsterCard? TryGetFieldMonsterForPetCombatId(Player? player, uint petCombatId)
    {
        if (player?.PlayerCombatState == null || petCombatId == 0)
            return null;
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet.CombatId != petCombatId)
                continue;
            return DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
        }

        return null;
    }
}
