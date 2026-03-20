using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class BaseSpellCard : YgoDuelistCard, IYgoCard
{
    private const int RaceKeywordBase = 20000;
    private static CardKeyword SetKeyword => (CardKeyword)10009;

    private static CardKeyword RaceToKeyword(DuelMonsterRace race)
        => (CardKeyword)(RaceKeywordBase + (int)race);

    public YgoCardType YgoCardType => YgoCardType.Spell;

    public DuelMonsterRace DuelMonsterRace { get; }

    protected BaseSpellCard(int cost, CardRarity rarity, TargetType target, DuelMonsterRace duelMonsterRace)
        : base(cost, CardType.Skill, rarity, target)
    {
        DuelMonsterRace = duelMonsterRace;
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

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[]
        {
            RaceToKeyword(DuelMonsterRace),
            SetKeyword,
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(RaceToKeyword(DuelMonsterRace)),
            HoverTipFactory.FromKeyword(SetKeyword),
        };
}
