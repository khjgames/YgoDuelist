using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Tracks delayed block from Total Defense Shogun's defend trigger until the owner's next turn start.
/// </summary>
public static class YgoTotalDefenseShogunDeferredBlock
{
    private static readonly Dictionary<Player, int> PendingBlockByPlayer = new();
    private static readonly object Gate = new();

    public static void Queue(Player player, int amount)
    {
        if (amount <= 0)
            return;
        lock (Gate)
        {
            PendingBlockByPlayer.TryGetValue(player, out int current);
            PendingBlockByPlayer[player] = current + amount;
        }
    }

    public static async Task ResolveAtTurnStartAsync(PlayerChoiceContext choiceContext, Player player)
    {
        int amount;
        lock (Gate)
        {
            if (!PendingBlockByPlayer.TryGetValue(player, out amount) || amount <= 0)
                return;
            PendingBlockByPlayer.Remove(player);
        }

        if (player.Creature == null)
            return;

        await CreatureCmd.GainBlock(player.Creature, amount, default, null);
    }

    public static void ClearAll()
    {
        lock (Gate)
            PendingBlockByPlayer.Clear();
    }
}
