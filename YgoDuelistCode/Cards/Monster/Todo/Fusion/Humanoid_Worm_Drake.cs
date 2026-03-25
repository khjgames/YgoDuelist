using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Humanoid_Worm_Drake : FusionMonsterCard
{
    public Humanoid_Worm_Drake()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 22,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Worm_Drake),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Humanoid_Slime))
    {
    }
}
