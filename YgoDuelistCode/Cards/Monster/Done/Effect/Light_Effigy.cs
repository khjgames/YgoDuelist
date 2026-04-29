using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Light_Effigy : EffectMonsterCard, IDoubleTributeMaterial
{
    public override int AttackPortionCount => 2;
    public Light_Effigy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 15,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            duelMonsterDefensePlayEnergyOverride: 0)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RequiresNormalMonster = true,
        RestrictAttribute = true,
        RequiredAttribute = DuelMonsterAttribute.Light
    };

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Normal;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Light_Effigy), DoubleTributeTargetSpec);
}
