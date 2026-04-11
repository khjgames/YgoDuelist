using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Localization.DynamicVars;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class The_Winged_Dragon_of_Ra : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly CardKeyword DoomedKeyword = (CardKeyword)20048;
    private static readonly CardKeyword RebirthKeyword = (CardKeyword)20052;

    public The_Winged_Dragon_of_Ra()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Divine,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.DivineBeast,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.God | YgoCardPackTags.Dragon | YgoCardPackTags.WinCon;

    public override Type[] RelatedCards => new[] { typeof(The_Winged_Dragon_of_Ra) };

    public override int ShopPriceModifier => 40;

    public override bool NormalSummonSkipsStiffFatigueOnSummonTurn => true;

    protected override int? TributeReleaseCountOverride => 3;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Mgc2", 10m);
            yield return new PowerVar<DoomPower>(0m);
            yield return new ComputedDecimalVar("CurrentDoom_Critical_Health", GetDoomGainPreview, 0m);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Append(DoomedKeyword).Append(RebirthKeyword);

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromKeyword(DoomedKeyword);
            yield return HoverTipFactory.FromKeyword(RebirthKeyword);
            yield return HoverTipFactory.FromPower<DoomPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-THE_WINGED_DRAGON_OF_RA.activated_effect.description";

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        await CreatureCmd.GainMaxHp(Owner.Creature, mgc);

        if (!Owner.Creature.HasPower<RaDoomedPower>())
            await PowerCmd.Apply<RaDoomedPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(1m);
        DynamicVars["Mgc2"].UpgradeValueBy(5m);
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        decimal gain = GetDoomGainPreview(source);
        if (gain > 0m)
            await PowerCmd.Apply<DoomPower>(player.Creature, gain, player.Creature, source);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (IsCanonical || Owner?.Creature == null)
            return (0, 0);
        int doom = (int)Owner.Creature.GetPowerAmount<DoomPower>();
        return (doom, doom);
    }

    private static decimal GetDoomGainPreview(CardModel card)
    {
        if (card == null || card.IsCanonical)
            return 0m;
        if (card.Owner?.Creature == null)
            return 0m;
        int hp = (int)card.Owner.Creature.CurrentHp;
        int target = Math.Max(0, hp - 1);
        decimal doom = card.Owner.Creature.GetPowerAmount<DoomPower>();
        return Math.Max(0m, target - doom);
    }
}
