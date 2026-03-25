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
}
