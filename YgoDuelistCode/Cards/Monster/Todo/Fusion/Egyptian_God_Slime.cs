using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Egyptian_God_Slime : FusionMonsterCard
{
    public Egyptian_God_Slime()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 30,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Humanoid_Slime),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual.Fortress_Whale))
    {
    }
}
