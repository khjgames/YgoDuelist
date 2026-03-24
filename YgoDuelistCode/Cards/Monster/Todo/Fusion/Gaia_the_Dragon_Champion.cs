using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Gaia_the_Dragon_Champion : FusionMonsterCard
{
    public Gaia_the_Dragon_Champion()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 26,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Gaia_The_Fierce_Knight),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Curse_of_Dragon))
    {
    }

}
