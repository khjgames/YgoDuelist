using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Man_Eating_Black_Shark : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Man_Eating_Black_Shark()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 21,
            baseDef: 13,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fish,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Sea_Kamen),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Gruesome_Goo),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Amazon_of_the_Seas))
    {
    }
}
