using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Wind_Effigy : EffectMonsterCard, IDoubleTributeMaterial
{
    public override int AttackPortionCount => 3;
    public Wind_Effigy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 18,
            baseDef: 2,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast,
            duelMonsterDefensePlayEnergyOverride: 0)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RequiresNormalMonster = true,
        RestrictAttribute = true,
        RequiredAttribute = DuelMonsterAttribute.Wind
    };

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Normal;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Wind_Effigy), DoubleTributeTargetSpec);
}
