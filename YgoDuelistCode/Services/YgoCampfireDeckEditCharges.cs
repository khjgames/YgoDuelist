using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per–rest-site bonus YGO deck edits: 4 on each campfire visit, +6 when choosing Deck Revamp (paid option).
/// </summary>
public static class YgoCampfireDeckEditCharges
{
    public const int FreeActionsPerRestVisit = 4;

    public const int DeckRevampBonusActions = 6;

    private static readonly ConditionalWeakTable<Player, StrongBox<int>> Table = new();

    public static void ResetForRestVisit(Player player)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(0));
        box.Value = FreeActionsPerRestVisit;
    }

    public static void GrantDeckRevampBonus(Player player)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(0));
        box.Value += DeckRevampBonusActions;
    }

    public static int GetRemaining(Player player)
    {
        return Table.TryGetValue(player, out StrongBox<int>? box) ? box.Value : 0;
    }

    /// <summary>
    /// If charges were never initialized for this player (ordering edge case before <see cref="ResetForRestVisit"/>), grant the standard visit allowance.
    /// Does not reset an existing entry (preserves spent bonus edits).
    /// </summary>
    public static void EnsureInitializedForRestSiteUi(Player player)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;
        if (Table.TryGetValue(player, out _))
            return;
        ResetForRestVisit(player);
    }

    public static bool TryConsumeOne(Player player)
    {
        if (!Table.TryGetValue(player, out StrongBox<int>? box) || box.Value <= 0)
            return false;

        box.Value--;
        return true;
    }

    public static void RefundOne(Player player)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(0));
        box.Value++;
    }
}
