using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Combat-scoped counters for passive powers; cleared in <see cref="Patches.YgoCombatEndClearPatch"/>.</summary>
public static class YgoDuelistPassivePowerState
{
    private static readonly Dictionary<Player, int> AccumulatedSpiritsLossesThisTurn = new();
    private static readonly Dictionary<Player, int> FortifiedBeastsTotalHpBonus = new();
    private static readonly Dictionary<Player, int> ChainSummoningSummonCount = new();

    public static void ClearAll()
    {
        AccumulatedSpiritsLossesThisTurn.Clear();
        FortifiedBeastsTotalHpBonus.Clear();
        ChainSummoningSummonCount.Clear();
    }

    public static void ResetAccumulatedSpiritsForTurn(Player player)
    {
        if (player != null)
            AccumulatedSpiritsLossesThisTurn[player] = 0;
    }

    public static void RegisterAccumulatedSpiritsFieldLoss(Player player)
    {
        if (player == null)
            return;
        AccumulatedSpiritsLossesThisTurn.TryGetValue(player, out int n);
        AccumulatedSpiritsLossesThisTurn[player] = n + 1;
    }

    public static int GetAccumulatedSpiritsLossesThisTurn(Player player) =>
        player != null && AccumulatedSpiritsLossesThisTurn.TryGetValue(player, out int n) ? n : 0;

    public static int AddFortifiedBeastsBonus(Player player, int delta)
    {
        if (player == null)
            return 0;
        FortifiedBeastsTotalHpBonus.TryGetValue(player, out int cur);
        int next = cur + delta;
        FortifiedBeastsTotalHpBonus[player] = next;
        return delta;
    }

    public static int GetFortifiedBeastsTotalBonus(Player player) =>
        player != null && FortifiedBeastsTotalHpBonus.TryGetValue(player, out int n) ? n : 0;

    public static int RegisterChainSummoningSummon(Player player, int everyN)
    {
        if (player == null || everyN <= 0)
            return 0;
        ChainSummoningSummonCount.TryGetValue(player, out int c);
        c++;
        ChainSummoningSummonCount[player] = c;
        if (c < everyN)
            return 0;
        ChainSummoningSummonCount[player] = c % everyN;
        return c / everyN;
    }
}
