using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoTributeSummonCardHooks
{
    public static async Task DispatchOnTributeSummonedMonsterAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        BaseMonsterCard summonedMonster,
        IReadOnlyList<BaseMonsterCard> tributeMonsters)
    {
        foreach (CardModel card in YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player)))
        {
            if (card is YgoDuelistCard ygo)
            {
                await ygo.OnTributeSummonedMonster(
                    choiceContext,
                    player,
                    summonedMonster,
                    tributeMonsters);
            }
        }
    }
}