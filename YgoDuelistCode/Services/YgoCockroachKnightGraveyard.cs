using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Cockroach_Knight"/>: when sent to the Graveyard, place it on top of the draw pile instead.
/// </summary>
public static class YgoCockroachKnightGraveyard
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Cockroach_Knight roach)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        TaskHelper.RunSafely(RunAsync(player, roach));
    }

    private static async Task RunAsync(Player player, Cockroach_Knight roach)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (draw == null || gy == null || !gy.Cards.Contains(roach))
            return;

        await CardPileCmd.Add(roach, draw, CardPilePosition.Top, roach, false);
    }
}
