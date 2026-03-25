using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>True while <see cref="Stumbling"/> is face-up in this player's Spell/Trap zone.</summary>
public static class YgoStumblingField
{
    public static bool IsActive(Player? player)
    {
        if (player == null)
            return false;

        foreach (var c in YgoFieldSpellStatAggregator.GetActiveFaceUpContinuousSpells(player))
        {
            if (c is Stumbling)
                return true;
        }

        return false;
    }
}
