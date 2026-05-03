using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// When a union equip would be destroyed (zone to graveyard), send the equip to Limbo and the paired monster from Limbo to the graveyard.
/// </summary>
public static class YgoUnionLimboDestroyed
{
    public static async Task AfterEquipArrivedInGraveyardFromZoneAsync(Player player, BaseEquipSpellCard equip, BaseMonsterCard limboMonster)
    {
        CardPile? limbo = YgoPlayerPiles.Limbo(player);
        CardPile? grave = YgoPlayerPiles.Graveyard(player);
        if (limbo == null || grave == null)
            return;
        if (equip is not IYgoUnionEquipSpell)
            return;
        if (limboMonster.Pile?.Type != LimboPile.CustomType)
            return;

        await CardPileCmd.Add(
            new CardModel[] { limboMonster },
            grave,
            CardPilePosition.Top,
            limboMonster,
            false);

        if (equip.Pile?.Type == GraveyardPile.CustomType)
        {
            await CardPileCmd.Add(
                new CardModel[] { equip },
                limbo,
                CardPilePosition.Top,
                equip,
                false);
        }

        YgoUnionLimboRegistry.RemovePairForEquip(equip);
        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }
}
