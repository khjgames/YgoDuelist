using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

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
        if (pile.Type != GraveyardPile.CustomType || !pile.IsCombatPile)
            return;
        if (CombatManager.Instance is not { IsInProgress: true })
            return;
        CombatState? cs = CombatManager.Instance.DebugOnlyGetState();
        if (cs == null)
            return;

        Player? player = ResolveGraveyardOwner(cs, pile) ?? addedCard.Owner;
        if (player?.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
            return;

        TaskHelper.RunSafely(RunAsync(player, marron));
    }

    private static Player? ResolveGraveyardOwner(CombatState cs, CardPile pile)
    {
        foreach (Player p in cs.Players)
        {
            if (GraveyardRelic.GetGraveyardPile(p) == pile)
                return p;
        }

        return null;
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
