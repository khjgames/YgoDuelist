using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoLimboService
{
    public static CardPile? GetPile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return YgoPlayerPiles.Limbo(player);
    }

    public static async Task SendToLimboAsync(Player player, CardModel card)
    {
        CardPile? pile = GetPile(player);
        if (pile == null || card.Pile == pile)
            return;

        await CardPileCmd.Add(
            new CardModel[] { card },
            pile,
            CardPilePosition.Top,
            card,
            false);
    }

    public static async Task RemoveAllFromCombat(Player player)
    {
        CardPile? pile = GetPile(player);
        if (pile == null || pile.Cards.Count == 0)
            return;

        IReadOnlyList<CardModel> snapshot = pile.Cards.ToList();
        foreach (CardModel c in snapshot)
            await CardPileCmd.RemoveFromCombat(c, false);
    }
}
