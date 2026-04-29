using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Meteor_Black_Dragon : FusionMonsterCard
{
    public Meteor_Black_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 35,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Red_Eyes_Black_Dragon),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Meteor_Dragon))
    {
    }
}
