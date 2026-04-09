using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// STS power card: <see cref="CardType.Power"/>, cast anim, effect, Graveyard. Does not implement YGO framing (<see cref="IYgoCard"/>).
/// </summary>
public abstract class BaseYgoPowerCard : YgoDuelistCard
{
    public override (float H, float S, float V)? CustomFrameTintHsv => (0f, 0f, 0.6f);

    protected BaseYgoPowerCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, CardType.Power, rarity, target)
    {
    }

    protected abstract Task OnPowerPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature != null)
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await OnPowerPlay(choiceContext, cardPlay);
        await SendThisPowerToGraveyard(choiceContext);
    }

    private async Task SendThisPowerToGraveyard(PlayerChoiceContext choiceContext)
    {
        Player? player = Owner;
        if (player == null)
            return;

        CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
        if (graveyard == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { this },
            graveyard,
            CardPilePosition.Top,
            this,
            false);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];
}
