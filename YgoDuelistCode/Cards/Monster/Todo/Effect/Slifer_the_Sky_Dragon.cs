using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Slifer_the_Sky_Dragon : EffectMonsterCard
{
    private static readonly CardKeyword SlifersPressureKeyword = (CardKeyword)20049;
    private static readonly CardKeyword SlifersPressurePlusKeyword = (CardKeyword)20050;

    private bool ShowSlifersPressurePlus =>
        IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None;

    public Slifer_the_Sky_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Divine,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.DivineBeast,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.God | YgoCardPackTags.Dragon | YgoCardPackTags.WinCon;

    public override Type[] RelatedCards => new[] { typeof(Slifer_the_Sky_Dragon) };

    public override int ShopPriceModifier => 40;

    public override bool UseAlternateUpgradedDescription => true;

    protected override int? TributeReleaseCountOverride => 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", 5m) });

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Append(ShowSlifersPressurePlus ? SlifersPressurePlusKeyword : SlifersPressureKeyword);

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromKeyword(
                ShowSlifersPressurePlus ? SlifersPressurePlusKeyword : SlifersPressureKeyword);
        }
    }

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;
        await SliferSkyDragonService.ApplySliferPressureToAllEnemiesAsync(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(3m);
        DynamicVars["Mgc2"].UpgradeValueBy(2m);
        SyncPermanentExecuteIncreaseVar();
    }

    private static int GetOtherCardsInHand(CardModel card)
    {
        if (card == null || card.IsCanonical)
            return 0;
        if (card.Owner == null)
            return 0;
        if (CombatManager.Instance?.IsInProgress != true)
            return 0;
        var hand = PileType.Hand.GetPile(card.Owner).Cards;
        return hand.Count(c => c != card);
    }

    private static decimal GetPrintedAtk(CardModel card, Slifer_the_Sky_Dragon m) =>
        card.DynamicVars?.Damage != null ? card.DynamicVars.Damage.BaseValue : m.BaseAtk;

    private static decimal GetPrintedDef(CardModel card, Slifer_the_Sky_Dragon m)
    {
        if (card.DynamicVars != null && card.DynamicVars.ContainsKey("Def"))
            return card.DynamicVars["Def"].BaseValue;
        if (card.DynamicVars?.Block != null)
            return card.DynamicVars.Block.BaseValue;
        return m.BaseDef;
    }

    private static decimal GetCalculatedAtk(CardModel card)
    {
        if (card is not Slifer_the_Sky_Dragon m)
            return 0;
        int others = GetOtherCardsInHand(card);
        decimal mgc = card.DynamicVars != null && card.DynamicVars.ContainsKey("Mgc")
            ? card.DynamicVars["Mgc"].BaseValue
            : m.BaseMgc;
        return GetPrintedAtk(card, m) + mgc * others;
    }

    private static decimal GetCalculatedDef(CardModel card)
    {
        if (card is not Slifer_the_Sky_Dragon m)
            return 0;
        int others = GetOtherCardsInHand(card);
        decimal mgc = card.DynamicVars != null && card.DynamicVars.ContainsKey("Mgc")
            ? card.DynamicVars["Mgc"].BaseValue
            : m.BaseMgc;
        return GetPrintedDef(card, m) + mgc * others;
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        int printedAtk = (int)GetPrintedAtk(this, this);
        int printedDef = (int)GetPrintedDef(this, this);
        int atk = (int)GetCalculatedAtk(this);
        int def = (int)GetCalculatedDef(this);
        return (atk - printedAtk, def - printedDef);
    }
}
