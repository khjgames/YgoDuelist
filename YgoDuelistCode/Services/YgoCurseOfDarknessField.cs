using System.Linq;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>True while <see cref="Curse_of_Darkness"/> is face-up in this player's Spell/Trap zone.</summary>
public static class YgoCurseOfDarknessField
{
    public static bool IsActive(Player? player)
    {
        if (player == null)
            return false;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return false;

        return zone.Cards.OfType<Curse_of_Darkness>().Any(c => !c.FaceDown);
    }

    /// <summary>Sum of <c>Mgc</c> from each face-up <see cref="Curse_of_Darkness"/> in the zone (multiple copies stack).</summary>
    public static decimal GetTotalMgcDamage(Player? player)
    {
        if (player == null)
            return 0m;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return 0m;

        decimal sum = 0m;
        foreach (Curse_of_Darkness c in zone.Cards.OfType<Curse_of_Darkness>())
        {
            if (!c.FaceDown)
                sum += c.DynamicVars["Mgc"].BaseValue;
        }

        return sum;
    }

    /// <summary>First face-up curse in zone order, for damage attribution.</summary>
    public static Curse_of_Darkness? GetFirstActiveCurse(Player? player)
    {
        if (player == null)
            return null;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return null;

        return zone.Cards.OfType<Curse_of_Darkness>().FirstOrDefault(c => !c.FaceDown);
    }
}
