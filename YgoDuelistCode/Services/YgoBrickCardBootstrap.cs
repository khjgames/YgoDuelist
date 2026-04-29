using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Moves <see cref="IYgoBrickCard"/> instances out of the draw and hand piles into the discard pile at combat start,
/// and strips them from the draw pile after recycle shuffles (<see cref="Patches.PatchesForCommandSystem.CardPileCmdShuffleBrickStripPatch"/>).
/// </summary>
public static class YgoBrickCardBootstrap
{
    public static void StripBricksFromPileToDiscard(CardPile? from, CardPile? discard)
    {
        if (from == null || discard == null)
            return;

        foreach (CardModel c in from.Cards.ToArray())
        {
            if (c is not IYgoBrickCard)
                continue;
            from.RemoveInternal(c, silent: true);
            discard.AddInternal(c, -1, silent: true);
        }

        from.InvokeContentsChanged();
        discard.InvokeContentsChanged();
    }

    public static void StripBricksFromPlayerCombatPiles(Player player)
    {
        if (player.PlayerCombatState == null)
            return;
        CardPile? draw = player.PlayerCombatState.DrawPile;
        CardPile? hand = player.PlayerCombatState.Hand;
        CardPile? dis = player.PlayerCombatState.DiscardPile;
        StripBricksFromPileToDiscard(draw, dis);
        StripBricksFromPileToDiscard(hand, dis);
    }
}
