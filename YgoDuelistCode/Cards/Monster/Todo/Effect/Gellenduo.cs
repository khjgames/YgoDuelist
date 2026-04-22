using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Gellenduo : EffectMonsterCard, IDoubleTributeMaterial
{
    public override int AttackPortionCount => 2;
    public Gellenduo()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 17,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            duelMonsterDefensePlayEnergyOverride: 0)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictAttribute = true,
        RequiredAttribute = DuelMonsterAttribute.Light,
        RestrictRace = true,
        RequiredRace = DuelMonsterRace.Fairy
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Light;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Gellenduo), DoubleTributeTargetSpec);
}
