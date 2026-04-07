using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared rules for monster Command Attack/Defend energy and matching hand-summon additions:
/// printed base, minus stacked per-source discounts (equips, linked traps, intrinsic via <see cref="BaseMonsterCard.GetDuelMonsterAttackPlayEnergyDiscount"/>),
/// plus stacked field-wide additions (e.g. +1 per face-up Narrow Pass).
/// </summary>
public static class YgoMonsterCommandEnergyModifiers
{
    /// <summary>
    /// Field-wide energy additions for Command Attack/Defend and hand ATK/DEF summons only — not field <c>Activate_Effect</c>. Stack Narrow Pass and future sources here.
    /// </summary>
    public static int SumFieldWideMonsterCommandEnergyAdd(Player? player) =>
        YgoNarrowPassField.GetMonsterCommandEnergyAdd(player);

    /// <summary>Command Attack row: same discount math as hand monster in attack stance, then field-wide adds.</summary>
    public static int GetFieldCommandAttackEnergyCost(NormalMonsterCard source)
    {
        int baseCost = source.DuelMonsterAttackPlayEnergy;
        int discount = source.GetDuelMonsterAttackPlayEnergyDiscount();
        int after = discount <= 0 ? baseCost : baseCost - discount;
        if (after < 0)
            after = 0;
        return after + SumFieldWideMonsterCommandEnergyAdd(source.Owner);
    }

    /// <summary>Command Defend row: same discount math as hand monster in defense stance, then field-wide adds.</summary>
    public static int GetFieldCommandDefendEnergyCost(NormalMonsterCard source)
    {
        int baseCost = source.DuelMonsterDefensePlayEnergy;
        int discount = source.GetDuelMonsterDefensePlayEnergyDiscount();
        int after = discount <= 0 ? baseCost : baseCost - discount;
        if (after < 0)
            after = 0;
        return after + SumFieldWideMonsterCommandEnergyAdd(source.Owner);
    }

    /// <summary>
    /// After hand monster energy is reduced by discounts, add field-wide command tax when summoning from hand (fusion excluded by base game rule).
    /// </summary>
    public static int ApplyHandSummonFieldWideAddIfApplicable(BaseMonsterCard bm, int costAfterDiscounts)
    {
        if (bm.IsMutable && bm.CanSummonDuelMonster && bm.YgoCardType != YgoCardType.FusionMonster)
            return costAfterDiscounts + SumFieldWideMonsterCommandEnergyAdd(bm.Owner);
        return costAfterDiscounts;
    }
}
