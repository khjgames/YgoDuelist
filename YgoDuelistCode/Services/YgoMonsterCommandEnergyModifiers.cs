using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared rules for monster Command Attack/Defend energy and matching hand-summon additions:
/// printed base, minus stacked per-source discounts (equips, linked traps, intrinsic via <see cref="BaseMonsterCard.GetDuelMonsterAttackPlayEnergyDiscountForFieldCommand"/>),
/// plus stacked field-wide additions (e.g. +1 per face-up Narrow Pass).
/// </summary>
public static class YgoMonsterCommandEnergyModifiers
{
    /// <summary>
    /// Field-wide energy additions for Command Attack/Defend and hand ATK/DEF summons only — not field <c>Activate_Effect</c>. Stack Narrow Pass and future sources here.
    /// </summary>
    public static int SumFieldWideMonsterCommandEnergyAdd(Player? player) =>
        YgoNarrowPassField.GetMonsterCommandEnergyAdd(player);

    /// <summary>Command Attack row: discounts for the attack command (not the monster&apos;s current field stance), then field-wide adds.</summary>
    public static int GetFieldCommandAttackEnergyCost(NormalMonsterCard source)
    {
        if (TryGetPetForFieldSource(source, out Creature? pet) && pet.GetPower<FleetingFollowupPower>() != null)
            return 0;

        int baseCost = source.DuelMonsterAttackPlayEnergy;
        int discount = source.GetDuelMonsterAttackPlayEnergyDiscountForFieldCommand();
        int after = discount <= 0 ? baseCost : baseCost - discount;
        if (after < 0)
            after = 0;
        return after + SumFieldWideMonsterCommandEnergyAdd(source.Owner);
    }

    private static bool TryGetPetForFieldSource(NormalMonsterCard source, out Creature? pet)
    {
        pet = null;
        Player? owner = source.Owner;
        if (owner?.PlayerCombatState == null)
            return false;
        pet = YgoMpCombatOrder.FirstPetWhere(
            owner.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, source));
        return pet != null;
    }

    /// <summary>Command Defend row: discounts for the defend command (not the monster&apos;s current field stance), then field-wide adds.</summary>
    public static int GetFieldCommandDefendEnergyCost(NormalMonsterCard source)
    {
        int baseCost = source.DuelMonsterDefensePlayEnergy;
        int discount = source.GetDuelMonsterDefensePlayEnergyDiscountForFieldCommand();
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
