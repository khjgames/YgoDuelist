using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Solo <see cref="Cards.Monster.Done.Effect.Fiber_Jar"/> FLIP: kill all duel pets, move hand + spell/trap zone + graveyard into draw, shuffle, draw 5.
/// </summary>
public static class YgoFiberJarSoloRecycle
{
    public static async Task RunAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.PlayerCombatState == null)
            return;

        var pcs = player.PlayerCombatState;

        List<Creature> pets = YgoMpCombatOrder.PetsSnapshotAliveDuelMonstersOrderedByCombatId(pcs);
        foreach (Creature pet in pets)
            await CreatureCmd.Kill(pet, force: true);

        CardPile draw = pcs.DrawPile;
        await MoveEntirePileToDrawAsync(choiceContext, player, YgoPlayerPiles.Hand(player), draw);
        await MoveEntirePileToDrawAsync(choiceContext, player, YgoPlayerPiles.SpellTrapZone(player), draw);
        await MoveEntirePileToDrawAsync(choiceContext, player, YgoPlayerPiles.Graveyard(player), draw);

        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
        await CardPileCmd.Draw(choiceContext, 5, player);
    }

    private static async Task MoveEntirePileToDrawAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardPile? from,
        CardPile draw)
    {
        if (from == null || from.Cards.Count == 0)
            return;

        IReadOnlyList<CardModel> snapshot = YgoMpCombatOrder.CardsSnapshotOrderedForMp(from.Cards);
        foreach (CardModel card in snapshot)
        {
            if (card.Pile != from)
                continue;
            await CardPileCmd.Add(card, draw, CardPilePosition.Bottom, card, false);
        }
    }
}
