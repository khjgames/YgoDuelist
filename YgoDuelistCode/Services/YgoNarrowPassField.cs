using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Face-up spell/trap zone contributors can add command taxes and repeated attack/defend resolutions.</summary>
public static class YgoNarrowPassField
{
    /// <summary>
    /// Spell/Trap zone <see cref="PileType"/> is only wired during combat; <see cref="CardPile.Get"/> throws off-map / in merchant / deck screens.
    /// </summary>
    private static CardPile? GetSpellTrapZoneInCombat(Player? player)
    {
        if (player == null || CombatManager.Instance?.IsInProgress != true)
            return null;
        return SpellTrapZonePile.CustomType.GetPile(player);
    }

    public static bool IsActive(Player? player)
    {
        CardPile? zone = GetSpellTrapZoneInCombat(player);
        if (zone == null)
            return false;

        return zone.Cards
            .OfType<IYgoMonsterCommandFieldTaxContributor>()
            .Any(c => c.IsMonsterCommandFieldTaxActive());
    }

    /// <summary>+1 energy per face-up Narrow Pass (stacking).</summary>
    public static int GetMonsterCommandEnergyAdd(Player? player) =>
        GetActiveCount(player);

    public static int GetActiveCount(Player? player)
    {
        CardPile? zone = GetSpellTrapZoneInCombat(player);
        if (zone == null)
            return 0;

        return zone.Cards
            .OfType<IYgoMonsterCommandFieldTaxContributor>()
            .Sum(c => c.GetMonsterCommandEnergyAdd());
    }

    public static int GetAttackOrDefendResolutionCount(Player? player) =>
        1 + GetAttackOrDefendResolutionAdd(player);

    private static int GetAttackOrDefendResolutionAdd(Player? player)
    {
        CardPile? zone = GetSpellTrapZoneInCombat(player);
        if (zone == null)
            return 0;

        return zone.Cards
            .OfType<IYgoMonsterCommandFieldTaxContributor>()
            .Sum(c => c.GetMonsterCommandResolutionAdd());
    }

    public static async Task ApplyMonsterCommandLifePaymentIfActiveAsync(
        PlayerChoiceContext choiceContext,
        Player? player,
        Creature? pet)
    {
        if (player == null || pet == null || !pet.IsAlive)
            return;

        CardModel? src = GetFirstActive(player);
        if (src == null)
            return;

        int divisor = ((IYgoMonsterCommandFieldTaxContributor)src).GetMonsterCommandLifePaymentDivisor();
        if (divisor <= 0)
            return;

        decimal maxHp = pet.MaxHp;
        if (maxHp <= 0m)
            return;

        decimal payment = decimal.Ceiling(maxHp / divisor);
        if (payment <= 0m)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            pet,
            payment,
            ValueProp.Unpowered | ValueProp.Unblockable,
            player.Creature,
            src);
    }

    public static CardModel? GetFirstActive(Player? player)
    {
        CardPile? zone = GetSpellTrapZoneInCombat(player);
        if (zone == null)
            return null;

        return zone.Cards
            .Where(c => c is IYgoMonsterCommandFieldTaxContributor hook && hook.IsMonsterCommandFieldTaxActive())
            .OrderBy(c => c.Id?.Entry ?? string.Empty, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
