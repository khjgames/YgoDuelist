using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Whirlwind_Prodigy : EffectMonsterCard, IDoubleTributeMaterial
{
    public Whirlwind_Prodigy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 15,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictAttribute = true,
        RequiredAttribute = DuelMonsterAttribute.Wind
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Wind;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Whirlwind_Prodigy), DoubleTributeTargetSpec);
}
