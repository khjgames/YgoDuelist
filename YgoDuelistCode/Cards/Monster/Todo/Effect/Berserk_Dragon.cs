using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Berserk_Dragon : EffectMonsterCard
{
    public Berserk_Dragon()
        : base(
            cost: 2,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 35,
            baseDef: 0,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Zombie,
            duelMonsterAttackPlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override Type[] BundledCards => new[] { typeof(A_Deal_with_Dark_Ruler) };

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate =>
        YgoDealWithDarkRulerState.IsDealWithDarkRulerSummonBypassActive;

    public override bool DuelMonsterAttackHitsAllEnemies => true;

    protected override bool IsPlayable => base.IsPlayable && CanSummonDuelMonster;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
