using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

internal static class YgoReplayFizzle
{
    public static async Task SendToGraveyardAsync(Player player, CardModel card)
    {
        if (player == null)
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { card },
            gy,
            CardPilePosition.Top,
            card,
            false);
    }
}
