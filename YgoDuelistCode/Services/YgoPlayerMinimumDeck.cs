using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YgoDuelist minimum deck size from <c>Deck_Trunk_Side_System</c> / pack and shop rewards.
/// Starts at <see cref="StartingMinimum"/>, gains +1 per six received cards, and loses one per voluntary removal.
/// </summary>
public static class YgoPlayerMinimumDeck
{
    public const int StartingMinimum = 14;
    public const int ReceivedCardsPerMinimumIncrease = 6;

    private static readonly ConditionalWeakTable<Player, StrongBox<int>> Table = new();
    private static readonly ConditionalWeakTable<Player, StrongBox<int>> ReceivedCardProgressTable = new();

    public static int Get(Player player)
    {
        return Table.TryGetValue(player, out StrongBox<int>? box) ? box.Value : StartingMinimum;
    }

    public static void SetLoadedFromSave(Player player, int value)
    {
        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
        box.Value = Math.Max(StartingMinimum, value);
    }

    public static int GetReceivedCardProgress(Player player)
    {
        return ReceivedCardProgressTable.TryGetValue(player, out StrongBox<int>? box) ? box.Value : 0;
    }

    public static void SetReceivedCardProgressLoadedFromSave(Player player, int value)
    {
        StrongBox<int> box = ReceivedCardProgressTable.GetValue(player, static _ => new StrongBox<int>(0));
        box.Value = Math.Clamp(value, 0, ReceivedCardsPerMinimumIncrease - 1);
    }

    public static int AddReceivedCardsFromPacksOrShop(Player player, int cardsReceived)
    {
        if (cardsReceived <= 0)
            return 0;

        StrongBox<int> progress = ReceivedCardProgressTable.GetValue(player, static _ => new StrongBox<int>(0));
        int total = progress.Value + cardsReceived;
        int bumps = total / ReceivedCardsPerMinimumIncrease;
        progress.Value = total % ReceivedCardsPerMinimumIncrease;

        if (bumps > 0)
        {
            StrongBox<int> min = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
            min.Value += bumps;
        }

        return bumps;
    }

    /// <summary>Undo uncommitted pack-choice progress when the player backs out to the pack choice screen.</summary>
    public static void RevertReceivedCardsFromPacksOrShop(Player player, int cardsReceived)
    {
        if (cardsReceived <= 0)
            return;

        StrongBox<int> progress = ReceivedCardProgressTable.GetValue(player, static _ => new StrongBox<int>(0));
        int current = progress.Value - cardsReceived;
        int minDeckDecrease = 0;
        while (current < 0)
        {
            current += ReceivedCardsPerMinimumIncrease;
            minDeckDecrease++;
        }

        progress.Value = current;
        if (minDeckDecrease > 0)
        {
            StrongBox<int> min = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
            min.Value = Math.Max(StartingMinimum, min.Value - minDeckDecrease);
        }
    }

    public static void DecreaseAfterVoluntaryRemovals(Player player, int cardsRemoved)
    {
        if (cardsRemoved <= 0)
            return;

        StrongBox<int> box = Table.GetValue(player, static _ => new StrongBox<int>(StartingMinimum));
        box.Value = Math.Max(StartingMinimum, box.Value - cardsRemoved);
    }
}
