using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class The_Trojan_Horse : EffectMonsterCard, IDoubleTributeMaterial
{
    public The_Trojan_Horse()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 16,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictAttribute = true,
        RequiredAttribute = DuelMonsterAttribute.Earth
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Earth;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(The_Trojan_Horse), DoubleTributeTargetSpec);
}
