using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoSpellCounterService
{
    public static int GetCount(Player? player)
    {
        if (player?.Creature == null)
            return 0;
        var p = player.Creature.GetPower<SpellCounterPower>();
        return p != null ? (int)p.Amount : 0;
    }

    public static async Task Add(Player player, int amount, CardModel? cardSource)
    {
        if (amount == 0 || player.Creature == null)
            return;
        await PowerCmd.Apply<SpellCounterPower>(player.Creature, amount, player.Creature, cardSource);
    }

    public static async Task Remove(Player player, int amount, CardModel? cardSource)
    {
        if (amount == 0 || player.Creature == null)
            return;
        var existing = player.Creature.GetPower<SpellCounterPower>();
        if (existing == null)
            return;
        await PowerCmd.ModifyAmount(existing, -amount, player.Creature, cardSource);
    }
}
