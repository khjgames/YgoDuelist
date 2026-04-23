using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Tracks/desync-fixes Thorns granted by face-up <see cref="Des_Counterblow"/> cards in the Spell/Trap zone.</summary>
public static class YgoDesCounterblowThornsSync
{
    private const decimal ThornsPerCopy = 4m;
    private static readonly Dictionary<Player, decimal> GrantedByCounterblowByPlayer = new();

    public static async Task SyncForPlayerAsync(Player player)
    {
        if (player.Creature == null)
            return;

        int activeCopies = YgoPlayerPiles.SpellTrapZone(player)?
            .Cards.OfType<Des_Counterblow>()
            .Count(c => !c.FaceDown) ?? 0;
        decimal desired = activeCopies * ThornsPerCopy;
        decimal currentGranted = GrantedByCounterblowByPlayer.GetValueOrDefault(player, 0m);

        if (desired > currentGranted)
        {
            decimal add = desired - currentGranted;
            await PowerCmd.Apply<ThornsPower>(player.Creature, add, player.Creature, null);
            GrantedByCounterblowByPlayer[player] = desired;
            return;
        }

        if (desired < currentGranted)
        {
            decimal remove = currentGranted - desired;
            ThornsPower? thorns = player.Creature.GetPower<ThornsPower>();
            if (thorns != null)
                await PowerCmd.ModifyAmount(thorns, -remove, null, null);
            GrantedByCounterblowByPlayer[player] = desired;
        }
    }

    public static void ClearAll() => GrantedByCounterblowByPlayer.Clear();
}
