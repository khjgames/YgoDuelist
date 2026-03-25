using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Karbonala_Warrior : FusionMonsterCard
{
    public Karbonala_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 15,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.M_Warrior_1),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.M_Warrior_2))
    {
    }
}
