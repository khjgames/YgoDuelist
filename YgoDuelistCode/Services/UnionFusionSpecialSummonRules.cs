using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Exact on-field material checks for union-style special summons from the Extra Deck.</summary>
public static class UnionFusionSpecialSummonRules
{
    public static bool PlayerHasFusionInExtraDeck<TFusion>(Player player)
        where TFusion : FusionMonsterCard
    {
        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        return extra != null && extra.Cards.Any(static c => c is TFusion);
    }

    public static bool TryGetExactFieldMaterials(
        Player player,
        IReadOnlyList<Type> requiredExactTypes,
        out List<BaseMonsterCard> materials)
    {
        materials = new List<BaseMonsterCard>();
        if (player?.PlayerCombatState == null || requiredExactTypes == null || requiredExactTypes.Count == 0)
            return false;

        var field = new List<BaseMonsterCard>();
        foreach (var pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is BaseMonsterCard bm)
                field.Add(bm);
        }

        foreach (Type t in requiredExactTypes)
        {
            BaseMonsterCard? found = null;
            foreach (BaseMonsterCard m in field)
            {
                if (m.GetType() != t)
                    continue;
                if (materials.Contains(m))
                    continue;
                found = m;
                break;
            }

            if (found == null)
            {
                materials.Clear();
                return false;
            }

            materials.Add(found);
        }

        return true;
    }
}
