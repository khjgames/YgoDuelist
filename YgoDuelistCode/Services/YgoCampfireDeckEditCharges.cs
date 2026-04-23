using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per–rest-site YGO deck edits at the campfire: 3 “store to trunk” and 3 “put in deck” actions (separate pools).
/// </summary>
public static class YgoCampfireDeckEditCharges
{
    public const int StoreTrunkPerVisit = 3;

    public const int PutInDeckPerVisit = 3;

    private sealed class ChargeState
    {
        public int StoreTrunk;
        public int PutInDeck;
    }

    private static readonly ConditionalWeakTable<Player, ChargeState> Table = new();

    public static void ResetForRestVisit(Player player)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return;

        ChargeState state = Table.GetValue(player, static _ => new ChargeState());
        state.StoreTrunk = StoreTrunkPerVisit;
        state.PutInDeck = PutInDeckPerVisit;
    }

    public static int GetStoreRemaining(Player player) =>
        Table.TryGetValue(player, out ChargeState? s) ? s.StoreTrunk : 0;

    public static int GetPutInDeckRemaining(Player player) =>
        Table.TryGetValue(player, out ChargeState? s) ? s.PutInDeck : 0;

    /// <summary>
    /// If charges were never initialized for this player (ordering edge case before <see cref="ResetForRestVisit"/>), grant the standard visit allowance.
    /// </summary>
    public static void EnsureInitializedForRestSiteUi(Player player)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return;
        if (Table.TryGetValue(player, out _))
            return;
        ResetForRestVisit(player);
    }

    public static void ConsumeStore(Player player, int amount)
    {
        if (amount <= 0 || !YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return;
        ChargeState state = Table.GetValue(player, static _ => new ChargeState());
        state.StoreTrunk = Math.Max(0, state.StoreTrunk - amount);
    }

    public static void ConsumePutInDeck(Player player, int amount)
    {
        if (amount <= 0 || !YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return;
        ChargeState state = Table.GetValue(player, static _ => new ChargeState());
        state.PutInDeck = Math.Max(0, state.PutInDeck - amount);
    }
}
