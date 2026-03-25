using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class ConvulsionOfNatureReveal
{
    public static async Task TryRevealDrawTop(PlayerChoiceContext choiceContext, Player player)
    {
        var draw = player.PlayerCombatState?.DrawPile;
        if (draw == null || draw.IsEmpty)
            return;

        CardModel top = draw.Cards[0];
        IReadOnlyList<CardModel> single = new List<CardModel> { top };
        await CardSelectCmd.FromChooseACardScreen(choiceContext, single, player, canSkip: true);
    }
}
