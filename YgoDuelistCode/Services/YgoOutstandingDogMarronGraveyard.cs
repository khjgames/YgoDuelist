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
/// <see cref="Outstanding_Dog_Marron"/>: when sent to the Graveyard, shuffle this card into your draw pile, then draw 1 (2 if upgraded).
/// </summary>
public static class YgoOutstandingDogMarronGraveyard
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Outstanding_Dog_Marron marron)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        TaskHelper.RunSafely(RunAsync(player, marron));
    }

    private static async Task RunAsync(Player player, Outstanding_Dog_Marron marron)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (draw == null || gy == null || !gy.Cards.Contains(marron))
            return;

        var ctx = new BlockingPlayerChoiceContext();

        await CardPileCmd.Add(marron, draw, CardPilePosition.Bottom, marron, false);
        await CardPileCmd.ShuffleIfNecessary(ctx, player);

        int draws = marron.CurrentUpgradeLevel >= 1 ? 2 : 1;
        await CardPileCmd.Draw(ctx, draws, player);
    }
}
