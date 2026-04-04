using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class DoubleTributeTributeMath
{
    /// <summary>
    /// Per-material tribute value toward <paramref name="summonBeingSummoned"/>.
    /// Double materials only apply when the summon needs two releases.
    /// </summary>
    public static int GetTributeContribution(BaseMonsterCard material, BaseMonsterCard summonBeingSummoned)
    {
        int need = summonBeingSummoned.TributeReleaseCount;
        if (need <= 1)
            return 1;
        if (material is IDoubleTributeMaterial dtm && dtm.DoubleTributeTargetSpec.Matches(summonBeingSummoned))
            return 2;
        return 1;
    }

    public static bool TributeFieldCardsMeetCost(BaseMonsterCard summon, IReadOnlyList<BaseMonsterCard> materials)
    {
        int need = summon.TributeReleaseCount;
        if (need <= 0)
            return true;
        if (materials.Count == 0 || materials.Count > need)
            return false;
        if (materials.Distinct().Count() != materials.Count)
            return false;
        int sum = materials.Sum(m => GetTributeContribution(m, summon));
        return sum == need;
    }

    public static bool TributePetsMeetCost(BaseMonsterCard summon, List<Creature>? mats)
    {
        int need = summon.TributeReleaseCount;
        if (need <= 0)
            return true;
        if (mats == null || mats.Count == 0 || mats.Count > need)
            return false;

        var cards = new List<BaseMonsterCard>(mats.Count);
        foreach (Creature pet in mats)
        {
            BaseMonsterCard? c = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (c == null)
                return false;
            cards.Add(c);
        }

        return TributeFieldCardsMeetCost(summon, cards);
    }

    /// <summary>Minimum number of distinct field monsters needed to pay <see cref="BaseMonsterCard.TributeReleaseCount"/>.</summary>
    public static int MinMonstersNeededToPayTribute(IReadOnlyList<BaseMonsterCard> field, BaseMonsterCard summon)
    {
        int need = summon.TributeReleaseCount;
        if (need <= 0)
            return 0;
        if (field.Count == 0)
            return int.MaxValue;
        if (need == 1)
            return 1;

        foreach (BaseMonsterCard c in field)
        {
            if (GetTributeContribution(c, summon) >= need)
                return 1;
        }

        if (field.Count < need)
            return int.MaxValue;

        return need;
    }
}
