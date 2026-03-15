using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class BaseSpellCard : YgoDuelistCard, IYgoCard
{
    public YgoCardType YgoCardType => YgoCardType.Spell;

    protected BaseSpellCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, CardType.Skill, rarity, target)
    {
    }

    /// <summary>
    /// Implement the actual effect of the spell here. Base class handles cast anim + sending to graveyard.
    /// </summary>
    protected abstract Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await OnSpellPlay(choiceContext, cardPlay);

        await SendThisSpellToGraveyard(choiceContext);
    }

    private async Task SendThisSpellToGraveyard(PlayerChoiceContext choiceContext)
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
