using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class BaseTrapCard : YgoDuelistCard, IYgoCard
{
    private const int RaceKeywordBase = 20000;
    private static CardKeyword SetKeyword => (CardKeyword)10009;
    private static CardKeyword TrapKeyword => (CardKeyword)10011;
    private static CardKeyword FaceDownKeyword => (CardKeyword)10012;

    private static CardKeyword RaceToKeyword(DuelMonsterRace race)
        => (CardKeyword)(RaceKeywordBase + (int)race);

    public YgoCardType YgoCardType => YgoCardType.Trap;
    public bool FaceDown { get; set; } = false;
    public bool WasSetIntoSpellTrapZone { get; protected set; } = false;

    public DuelMonsterRace DuelMonsterRace { get; }

    protected BaseTrapCard(int cost, CardRarity rarity, TargetType target, DuelMonsterRace duelMonsterRace)
        : base(cost, CardType.Skill, rarity, target)
    {
        DuelMonsterRace = duelMonsterRace;
    }

    private bool ShouldShowRaceKeyword => DuelMonsterRace != DuelMonsterRace.TrapNormal;

    protected abstract Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        WasSetIntoSpellTrapZone = false;
        FaceDown = false;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OnTrapPlay(choiceContext, cardPlay);
        await SendThisTrapToGraveyard(choiceContext);
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            if (Pile?.Type == PileType.Hand)
                return false;

            if (Pile?.Type == PileType.Hand && Owner != null && !YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(Owner, this))
                return false;

            return true;
        }
    }

    public void EnterSpellTrapZoneAsSetCard()
    {
        WasSetIntoSpellTrapZone = true;
        FaceDown = true;
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

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (ShouldShowRaceKeyword)
            {
                var keywords = new List<CardKeyword>
                {
                    RaceToKeyword(DuelMonsterRace),
                    SetKeyword,
                    TrapKeyword,
                };
                if (WasSetIntoSpellTrapZone)
                    keywords.Add(FaceDownKeyword);
                return keywords;
            }

            var fallback = new List<CardKeyword>
            {
                SetKeyword,
                TrapKeyword,
            };
            if (WasSetIntoSpellTrapZone)
                fallback.Add(FaceDownKeyword);
            return fallback;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (ShouldShowRaceKeyword)
            {
                var tips = new List<IHoverTip>
                {
                    HoverTipFactory.FromKeyword(RaceToKeyword(DuelMonsterRace)),
                    HoverTipFactory.FromKeyword(SetKeyword),
                    HoverTipFactory.FromKeyword(TrapKeyword),
                };
                if (WasSetIntoSpellTrapZone)
                    tips.Add(HoverTipFactory.FromKeyword(FaceDownKeyword));
                return tips;
            }

            var fallback = new List<IHoverTip>
            {
                HoverTipFactory.FromKeyword(SetKeyword),
                HoverTipFactory.FromKeyword(TrapKeyword),
            };
            if (WasSetIntoSpellTrapZone)
                fallback.Add(HoverTipFactory.FromKeyword(FaceDownKeyword));
            return fallback;
        }
    }
}
