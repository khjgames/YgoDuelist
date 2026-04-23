using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoBanishedService
{
    public static CardPile? GetPile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return YgoPlayerPiles.Banished(player);
    }

    public static async Task BanishCard(Player player, CardModel card)
    {
        var pile = GetPile(player);
        if (pile == null)
            return;

        if (card is IYgoApplyAncientChantPowerWhenBanishedFromGraveyard chant
            && card.Pile?.Type == GraveyardPile.CustomType
            && player.Creature != null)
            await chant.ApplyPowerWhenBanishedFromGraveyardAsync(player);

        await CardPileCmd.Add(
            new CardModel[] { card },
            pile,
            CardPilePosition.Top,
            card,
            false);
    }

    public static async Task RemoveAllFromCombat(Player player)
    {
        var pile = GetPile(player);
        if (pile == null || pile.Cards.Count == 0)
            return;

        IReadOnlyList<CardModel> snapshot = pile.Cards.ToList();
        foreach (CardModel c in snapshot)
            await CardPileCmd.RemoveFromCombat(c, false);
    }
}
