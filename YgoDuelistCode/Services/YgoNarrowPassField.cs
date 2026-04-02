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
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Face-up <see cref="Narrow_Pass"/> in the Spell/Trap zone: +1 monster play/command energy, summon pet life payment, and repeated attack/defend resolutions (field commands and hand normal/tribute ATK/DEF summons via <see cref="AbstractMonsterCard.CanonicalEnergyCost"/> / <see cref="NormalMonsterCard"/>).</summary>
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

        return zone.Cards.OfType<Narrow_Pass>().Any(c => !c.FaceDown);
    }

    public static int GetMonsterCommandEnergyAdd(Player? player) =>
        IsActive(player) ? 1 : 0;

    public static int GetActiveCount(Player? player)
    {
        CardPile? zone = GetSpellTrapZoneInCombat(player);
        if (zone == null)
            return 0;

        return zone.Cards.OfType<Narrow_Pass>().Count(c => !c.FaceDown);
    }

    public static int GetAttackOrDefendResolutionCount(Player? player) =>
        1 + GetActiveCount(player);

    public static async Task ApplyMonsterCommandLifePaymentIfActiveAsync(
        PlayerChoiceContext choiceContext,
        Player? player,
        Creature? pet)
    {
        if (player == null || pet == null || !pet.IsAlive)
            return;

        Narrow_Pass? src = GetFirstActive(player);
        if (src == null)
            return;

        int divisor = (int)src.DynamicVars["Mgc"].BaseValue;
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

    public static Narrow_Pass? GetFirstActive(Player? player)
    {
        CardPile? zone = GetSpellTrapZoneInCombat(player);
        if (zone == null)
            return null;

        return zone.Cards.OfType<Narrow_Pass>().FirstOrDefault(c => !c.FaceDown);
    }
}
