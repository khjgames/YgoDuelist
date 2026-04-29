using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Totem_Dragon : EffectMonsterCard, IDoubleTributeMaterial
{
    public Totem_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 4,
            baseDef: 2,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            duelMonsterAttackPlayEnergyOverride: 0,
            duelMonsterDefensePlayEnergyOverride: 0)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictRace = true,
        RequiredRace = DuelMonsterRace.Dragon
    };

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Dragon;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Totem_Dragon), DoubleTributeTargetSpec);
}
