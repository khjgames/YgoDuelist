using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class BaseSpellCard : YgoDuelistCard, IYgoCard
{
    private const int RaceKeywordBase = 20000;
    private static CardKeyword SetKeyword => (CardKeyword)10009;
    private static CardKeyword FaceDownKeyword => (CardKeyword)10012;
    private static CardKeyword CycleSpellKeyword => (CardKeyword)20040;
    private static CardKeyword SplinterKeyword => (CardKeyword)20044;
    private static CardKeyword BlightKeyword => (CardKeyword)20045;
    private static CardKeyword RecklessKeyword => (CardKeyword)20043;

    private static CardKeyword RaceToKeyword(DuelMonsterRace race)
        => (CardKeyword)(RaceKeywordBase + (int)race);

    public YgoCardType YgoCardType => YgoCardType.Spell;
    public bool FaceDown { get; set; } = false;
    public bool IsSetModeInHand { get; private set; } = false;
    public bool WasSetIntoSpellTrapZone { get; private set; } = false;

    public DuelMonsterRace DuelMonsterRace { get; }

    protected BaseSpellCard(int cost, CardRarity rarity, TargetType target, DuelMonsterRace duelMonsterRace)
        : base(cost, CardType.Skill, rarity, target)
    {
        DuelMonsterRace = duelMonsterRace;
    }

    
    protected BaseSpellCard(int cost, CardType cardType, CardRarity rarity, TargetType target, DuelMonsterRace duelMonsterRace)
        : base(cost, cardType, rarity, target)
    {
        DuelMonsterRace = duelMonsterRace;
    }

    /// <summary>
    /// Implement the actual effect of the spell here. Base class handles cast anim + sending to graveyard.
    /// </summary>
    protected abstract Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ColdWaveSpellTrapLockGate.MarkPlayerUsedSpellTrapThisTurn(Owner);
        PrepareSpellForActiveFieldZone();
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await OnSpellPlay(choiceContext, cardPlay);

        await YgoCurseOfDarknessSpellHook.AfterSpellResolved(choiceContext, this);

        await SendThisSpellToGraveyard(choiceContext);
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            if (Owner != null && ColdWaveSpellTrapLockGate.IsPlayerLockedThisTurn(Owner))
                return false;

            if (Pile?.Type == PileType.Hand && IsSetModeInHand)
                return false;

            if (Pile?.Type == PileType.Hand && Owner != null && !YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(Owner, this))
                return false;

            return true;
        }
    }

    public void ToggleSetSkillModeInHand()
    {
        if (Pile?.Type != PileType.Hand)
            return;
        IsSetModeInHand = !IsSetModeInHand;
    }

    public void EnterSpellTrapZoneAsSetCard()
    {
        IsSetModeInHand = false;
        WasSetIntoSpellTrapZone = true;
        FaceDown = true;
    }

    /// <summary>Face-up field spell in the zone (not a set card).</summary>
    protected void PrepareSpellForActiveFieldZone()
    {
        IsSetModeInHand = false;
        WasSetIntoSpellTrapZone = false;
        FaceDown = false;
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

    /// <summary>Full cast + GY resolution for <see cref="Double_Spell"/> replaying a spell from the Graveyard (card should already be in hand).</summary>
    internal async Task ResolveAsDoubleSpellReplayAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PrepareSpellForActiveFieldZone();
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OnSpellPlay(choiceContext, cardPlay);
        await YgoCurseOfDarknessSpellHook.AfterSpellResolved(choiceContext, this);
        await SendThisSpellToGraveyard(choiceContext);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[]
        {
            RaceToKeyword(DuelMonsterRace),
            SetKeyword,
        }.Concat(GetFaceDownKeyword()).Concat(GetCycleSpellKeywordWhenEligible()).Concat(GetSplinterBlightKeywords());

    private IEnumerable<CardKeyword> GetSplinterBlightKeywords()
    {
        if (CardShowsSplinterKeyword)
            yield return SplinterKeyword;
        if (CardShowsBlightKeyword)
            yield return BlightKeyword;
        if (CardShowsRecklessKeyword)
            yield return RecklessKeyword;
    }

    private IEnumerable<CardKeyword> GetFaceDownKeyword()
    {
        if (WasSetIntoSpellTrapZone)
            yield return FaceDownKeyword;
    }

    private IEnumerable<CardKeyword> GetCycleSpellKeywordWhenEligible()
    {
        if (Pile != null && YgoCycleKeywordPileHelper.AllowsCycleKeywordHints(Pile.Type))
            yield return CycleSpellKeyword;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>
            {
                HoverTipFactory.FromKeyword(RaceToKeyword(DuelMonsterRace)),
                HoverTipFactory.FromKeyword(SetKeyword),
            };
            if (WasSetIntoSpellTrapZone)
                tips.Add(HoverTipFactory.FromKeyword(FaceDownKeyword));
            foreach (CardKeyword kw in GetCycleSpellKeywordWhenEligible())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (CardKeyword kw in GetSplinterBlightKeywords())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (IHoverTip tip in EnumerateReferencedCardPreviewHoverTips())
                tips.Add(tip);
            return tips;
        }
    }
}
