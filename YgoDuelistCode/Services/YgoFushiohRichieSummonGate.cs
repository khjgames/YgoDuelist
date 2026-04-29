using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <c>Fushioh Richie</c> may only be Special Summoned while this flag is set (by <c>Great Dezard</c>'s activated effect).
/// </summary>
public static class YgoFushiohRichieSummonGate
{
    private static readonly HashSet<Player> _pending = new();

    public static void Grant(Player? player)
    {
        if (player != null)
            _pending.Add(player);
    }

    public static bool AllowsSpecialSummon(Player? player, BaseMonsterCard? card)
    {
        if (card?.GetType() != typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Fushioh_Richie))
            return true;
        return player != null && _pending.Contains(player);
    }

    public static void Consume(Player? player)
    {
        if (player != null)
            _pending.Remove(player);
    }

    public static void ResetForPlayer(Player? player)
    {
        if (player != null)
            _pending.Remove(player);
    }

    public static void ClearAll() => _pending.Clear();
}
