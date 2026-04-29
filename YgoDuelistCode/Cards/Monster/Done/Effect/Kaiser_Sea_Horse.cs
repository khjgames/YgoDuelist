using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Kaiser_Sea_Horse : EffectMonsterCard, IDoubleTributeMaterial
{
    public Kaiser_Sea_Horse()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 17,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.SeaSerpent)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictAttribute = true,
        RequiredAttribute = DuelMonsterAttribute.Light
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Light;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Kaiser_Sea_Horse), DoubleTributeTargetSpec);
}
