using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Master_of_Oz : FusionMonsterCard
{
    public Master_of_Oz()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 9,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 42,
            baseDef: 37,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Big_Koala),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Des_Kangaroo))
    {
    }
}
