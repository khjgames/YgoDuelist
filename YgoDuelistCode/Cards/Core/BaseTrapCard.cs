using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
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
    private static CardKeyword SplinterKeyword => (CardKeyword)20043;
    private static CardKeyword BlightKeyword => (CardKeyword)20044;

    /// <summary>
    /// <see cref="CardModel.Keywords"/> only unions <see cref="CanonicalKeywords"/> once; trap presentation depends on pile,
    /// so we keep keyword 10012 in sync like <see cref="AbstractMonsterCard.UpdateFaceDownKeywordFromBool"/>.
    /// </summary>
    private static readonly FieldInfo? CardModelKeywordsField =
        typeof(CardModel).GetField("_keywords", BindingFlags.NonPublic | BindingFlags.Instance);

    private static CardKeyword RaceToKeyword(DuelMonsterRace race)
        => (CardKeyword)(RaceKeywordBase + (int)race);

    public YgoCardType YgoCardType => YgoCardType.Trap;
    public bool FaceDown { get; set; } = false;
    /// <summary>
    /// Facedown traps set into the spell/trap zone cannot be activated until your next turn start clears this (YGO set timing).
    /// </summary>
    public bool SetThisTurn { get; set; } = true;
    public bool WasSetIntoSpellTrapZone { get; protected set; } = false;
    protected virtual bool CanActivateDirectlyFromHand => false;

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
        SetThisTurn = false;
        WasSetIntoSpellTrapZone = false;
        FaceDown = false;
        SyncFaceDownPresentationKeyword();
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

            if (Pile?.Type == PileType.Hand && !CanActivateDirectlyFromHand)
                return false;

            if (Pile?.Type == PileType.Hand && !CanActivateDirectlyFromHand && Owner != null && !YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(Owner, this))
                return false;

            if (SetThisTurn
                && Pile?.Type == SpellTrapZonePile.CustomType
                && WasSetIntoSpellTrapZone
                && FaceDown)
                return false;

            return true;
        }
    }

    public void EnterSpellTrapZoneAsSetCard()
    {
        SetThisTurn = true;
        WasSetIntoSpellTrapZone = true;
        FaceDown = true;
        SyncFaceDownPresentationKeyword();
    }

    /// <summary>
    /// At the start of your turn, facedown set traps in your spell/trap zone become activatable (clear <see cref="SetThisTurn"/>).
    /// </summary>
    public static void ClearSetThisTurnForFacedownSetTrapsInZone(Player player)
    {
        var pile = SpellTrapZonePile.CustomType.GetPile(player);
        if (pile == null)
            return;

        foreach (var model in pile.Cards)
        {
            if (model is not BaseTrapCard trap)
                continue;
            if (!trap.WasSetIntoSpellTrapZone || !trap.FaceDown)
                continue;
            trap.SetThisTurn = false;
        }
    }

    public void NormalizeFaceDownStateForCurrentPile()
    {
        var pileType = Pile?.Type;

        if (pileType == PileType.Hand)
        {
            // Trap cards in hand should always render as set/facedown unless explicitly exempt.
            WasSetIntoSpellTrapZone = false;
            FaceDown = !CanActivateDirectlyFromHand;
        }
        else if (pileType == SpellTrapZonePile.CustomType)
        {
            FaceDown = WasSetIntoSpellTrapZone;
        }
        else
        {
            // Outside spell/trap zone, trap cards are never considered set into zone.
            WasSetIntoSpellTrapZone = false;
            FaceDown = false;
        }

        SyncFaceDownPresentationKeyword();
    }

    /// <summary>
    /// Writes <see cref="FaceDownKeyword"/> into <see cref="CardModel.Keywords"/> to match <see cref="ShouldUseFaceDownPresentation"/>.
    /// Canonical instances cannot use <see cref="AddKeyword"/>; clearing the keyword cache forces a rebuild from <see cref="CanonicalKeywords"/>.
    /// </summary>
    public void SyncFaceDownPresentationKeyword()
    {
        if (!IsMutable)
        {
            CardModelKeywordsField?.SetValue(this, null);
            _ = Keywords;
            return;
        }

        _ = Keywords;
        RemoveKeyword(FaceDownKeyword);
        if (ShouldUseFaceDownPresentation())
            AddKeyword(FaceDownKeyword);
    }

    /// <summary>
    /// Trap cards present as facedown by default (including compendium/library views).
    /// They only present as face-up while actively face-up in the spell/trap zone.
    /// </summary>
    public bool ShouldUseFaceDownPresentation()
    {
        var pileType = Pile?.Type;
        if (pileType == SpellTrapZonePile.CustomType)
            return WasSetIntoSpellTrapZone || FaceDown;

        if (pileType == PileType.Hand)
            return !CanActivateDirectlyFromHand || FaceDown;

        // Everywhere else (compendium, draw/discard, etc.) defaults to facedown presentation.
        return true;
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
                if (ShouldUseFaceDownPresentation())
                    keywords.Add(FaceDownKeyword);
                keywords.AddRange(GetSplinterBlightKeywords());
                return keywords;
            }

            var fallback = new List<CardKeyword>
            {
                SetKeyword,
                TrapKeyword,
            };
            if (ShouldUseFaceDownPresentation())
                fallback.Add(FaceDownKeyword);
            fallback.AddRange(GetSplinterBlightKeywords());
            return fallback;
        }
    }

    private IEnumerable<CardKeyword> GetSplinterBlightKeywords()
    {
        if (CardShowsSplinterKeyword)
            yield return SplinterKeyword;
        if (CardShowsBlightKeyword)
            yield return BlightKeyword;
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
                if (ShouldUseFaceDownPresentation())
                    tips.Add(HoverTipFactory.FromKeyword(FaceDownKeyword));
                foreach (CardKeyword kw in GetSplinterBlightKeywords())
                    tips.Add(HoverTipFactory.FromKeyword(kw));
                return tips;
            }

            var fallback = new List<IHoverTip>
            {
                HoverTipFactory.FromKeyword(SetKeyword),
                HoverTipFactory.FromKeyword(TrapKeyword),
            };
            if (ShouldUseFaceDownPresentation())
                fallback.Add(HoverTipFactory.FromKeyword(FaceDownKeyword));
            foreach (CardKeyword kw in GetSplinterBlightKeywords())
                fallback.Add(HoverTipFactory.FromKeyword(kw));
            return fallback;
        }
    }
}
