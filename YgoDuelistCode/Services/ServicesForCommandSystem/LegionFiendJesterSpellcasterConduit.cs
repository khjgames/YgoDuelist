using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While you control <see cref="Legion_the_Fiend_Jester"/>, up to that many Spellcaster monsters per turn
/// ignore conduit (star) cost on normal/tribute summons from hand. Quota is <c>used &lt; Legion count</c> on the field,
/// so which Spellcasters use the free slots can be chosen in any order.
/// </summary>
public static class LegionFiendJesterSpellcasterConduit
{
    private static readonly Dictionary<Player, int> _waivedNormalSpellcasterSummonsThisTurn = new();

    public static int CountLegionsOnField(Player? player)
    {
        if (player == null)
            return 0;
        int n = 0;
        foreach (BaseMonsterCard? m in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (m is Legion_the_Fiend_Jester)
                n++;
        }

        return n;
    }

    public static int GetWaivedSummonsUsedThisTurn(Player? player) =>
        player != null && _waivedNormalSpellcasterSummonsThisTurn.TryGetValue(player, out int u) ? u : 0;

    public static void ResetForPlayer(Player? player)
    {
        if (player != null)
            _waivedNormalSpellcasterSummonsThisTurn.Remove(player);
    }

    public static void ClearAll() => _waivedNormalSpellcasterSummonsThisTurn.Clear();

    /// <summary>Hand preview / playability: waive conduit stars for the next eligible Spellcaster normal summon.</summary>
    public static bool ShouldWaiveConduitStarCostForHandSpellcaster(BaseMonsterCard card)
    {
        // Owner/Pile assert mutable; canonical templates (e.g. card library grid) must not touch them.
        if (card.IsCanonical)
            return false;
        if (card.DuelMonsterRace != DuelMonsterRace.Spellcaster || card.Owner == null)
            return false;
        if (card.Pile?.Type != PileType.Hand)
            return false;
        if (!card.CanSummonDuelMonster)
            return false;

        int legions = CountLegionsOnField(card.Owner);
        if (legions <= 0)
            return false;

        int used = GetWaivedSummonsUsedThisTurn(card.Owner);
        return used < legions;
    }

    /// <summary>
    /// After a successful normal/tribute summon, consume one waiver slot if this summon qualified.
    /// <paramref name="legionCountBeforeSummon"/> is from before the new monster was registered.
    /// </summary>
    public static void RegisterWaivedSummonAfterNormalSpellcasterSummon(
        Player player,
        BaseMonsterCard card,
        bool canAttackThisTurn,
        int legionCountBeforeSummon)
    {
        if (canAttackThisTurn
            || player.Creature == null
            || card.DuelMonsterRace != DuelMonsterRace.Spellcaster
            || legionCountBeforeSummon <= 0)
            return;

        int used = GetWaivedSummonsUsedThisTurn(player);
        if (used >= legionCountBeforeSummon)
            return;

        _waivedNormalSpellcasterSummonsThisTurn[player] = used + 1;
    }
}
