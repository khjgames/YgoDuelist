using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YgoDuelist minimum deck size from <c>Deck_Trunk_Side_System</c> / pack rewards: starts at 16, +1 per confirmed pack, −N when removing N cards via reward or Cook.
/// </summary>
public static class YgoPlayerMinimumDeck
{
    public const int StartingMinimum = 16;

    private static readonly ConditionalWeakTable<Player, StrongBox<int>> Table = new();

    public static int Get(Player player)
    {
        return Table.TryGetValue(player, out StrongBox<int>? box) ? box.Value : StartingMinimum;
    }

    public static void SetLoadedFromSave(Player player, int value)
    {
        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
        box.Value = Math.Max(StartingMinimum, value);
    }

    public static void IncreaseAfterPackRewardConfirmed(Player player)
    {
        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
        box.Value++;
    }

    /// <summary>Undo <see cref="IncreaseAfterPackRewardConfirmed"/> when the player backs out of deck assignment back to pack choice.</summary>
    public static void RevertLastPackOpenBump(Player player)
    {
        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
        box.Value = Math.Max(StartingMinimum, box.Value - 1);
    }

    public static void DecreaseAfterVoluntaryRemovals(Player player, int cardsRemoved)
    {
        if (cardsRemoved <= 0)
            return;

        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
        box.Value = Math.Max(StartingMinimum, box.Value - cardsRemoved);
    }
}
