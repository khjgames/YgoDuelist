using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoSealmasterMeiseiGate
{
    public static bool HasFaceUpSealmaster(Player? player) =>
        player != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(player).Any(m => m is Sealmaster_Meisei && !m.FaceDown);

    public static async Task DestroyTalismansIfNoSealmaster(Player? player)
    {
        if (player == null || HasFaceUpSealmaster(player))
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (zone == null || gy == null)
            return;

        CardModel[] toDestroy = zone.Cards
            .Where(c => c is Talisman_of_Trap_Sealing or Talisman_of_Spell_Sealing)
            .ToArray();
        if (toDestroy.Length == 0)
            return;

        await CardPileCmd.Add(toDestroy, gy, CardPilePosition.Top, toDestroy[0], false);
        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
    }
}
