using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;

public sealed class Wings_of_Wicked_Flame : NormalMonsterCard
{
    public Wings_of_Wicked_Flame()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 7,
            baseDef: 6,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }
}
