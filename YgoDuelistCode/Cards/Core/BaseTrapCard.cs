using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class BaseTrapCard : YgoDuelistCard, IYgoCard
{
    public YgoCardType YgoCardType => YgoCardType.Trap;

    protected BaseTrapCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, CardType.Skill, rarity, target)
    {
    }

    protected abstract Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OnTrapPlay(choiceContext, cardPlay);
        await SendThisTrapToGraveyard(choiceContext);
    }

    private async Task SendThisTrapToGraveyard(PlayerChoiceContext choiceContext)
    {
        var player = Owner;
        if (player == null)
            return;

        var graveyardPile = GraveyardPile.CustomType.GetPile(player);
        if (graveyardPile == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { this },
            graveyardPile,
            CardPilePosition.Top,
            this,
            false);
    }
}
