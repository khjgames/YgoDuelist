using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Trunk/side sell values: common 0.5, uncommon 1, rare 5 (batch payout uses floor to int gold).</summary>
public static class YgoMerchantSellService
{
    public static bool IsSellEligible(CardModel c) =>
        c is BaseMonsterCard or BaseSpellCard or BaseTrapCard;

    public static List<CardModel> ListSellable(Player player)
    {
        var list = new List<CardModel>();
        foreach (CardModel c in YgoPlayerRunPiles.Trunk(player)?.Cards ?? [])
        {
            if (IsSellEligible(c))
                list.Add(c);
        }

        foreach (CardModel c in YgoPlayerRunPiles.SideDeck(player)?.Cards ?? [])
        {
            if (IsSellEligible(c))
                list.Add(c);
        }

        return list;
    }

    public static void Tally(IEnumerable<CardModel> selected, out int commons, out int uncommons, out int rares, out double exactTotal, out int flooredGold)
    {
        commons = uncommons = rares = 0;
        exactTotal = 0;
        foreach (CardModel c in selected)
        {
            switch (c.Rarity)
            {
                case CardRarity.Rare:
                    rares++;
                    exactTotal += 5;
                    break;
                case CardRarity.Uncommon:
                    uncommons++;
                    exactTotal += 1;
                    break;
                default:
                    commons++;
                    exactTotal += 0.5;
                    break;
            }
        }

        flooredGold = (int)System.Math.Floor(exactTotal + 1e-9);
    }

    public static void RemoveFromTrunkOrSide(Player player, CardModel card)
    {
        CardPile? trunk = YgoPlayerRunPiles.Trunk(player);
        if (trunk == null)
            return;
        if (trunk.Cards.Contains(card))
        {
            trunk.RemoveInternal(card, silent: true);
            return;
        }

        YgoPlayerRunPiles.SideDeck(player)?.RemoveInternal(card, silent: true);
    }
}
