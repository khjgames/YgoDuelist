using System.Linq;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>True while a face-up card contributes post-spell owner damage in this player's Spell/Trap zone.</summary>
public static class YgoCurseOfDarknessField
{
    public static bool IsActive(Player? player)
    {
        if (player == null)
            return false;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return false;

        return zone.Cards
            .OfType<IYgoSpellResolvedOwnerDamageContributor>()
            .Any(c => c.GetOwnerSpellResolvedDamageAmount() > 0m);
    }

    /// <summary>Sum of spell-resolved owner damage contributed by each face-up zone card (multiple copies stack).</summary>
    public static decimal GetTotalMgcDamage(Player? player)
    {
        if (player == null)
            return 0m;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return 0m;

        decimal sum = 0m;
        foreach (IYgoSpellResolvedOwnerDamageContributor hook in zone.Cards.OfType<IYgoSpellResolvedOwnerDamageContributor>())
        {
            sum += hook.GetOwnerSpellResolvedDamageAmount();
        }

        return sum;
    }

    /// <summary>First face-up contributor in zone order, for damage attribution.</summary>
    public static CardModel? GetFirstActiveContributor(Player? player)
    {
        if (player == null)
            return null;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return null;

        foreach (CardModel c in zone.Cards)
        {
            if (c is IYgoSpellResolvedOwnerDamageContributor hook
                && hook.GetOwnerSpellResolvedDamageAmount() > 0m)
            {
                return c;
            }
        }

        return null;
    }
}
