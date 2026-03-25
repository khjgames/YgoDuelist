using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;

public sealed class Archfiend_Marmot_of_Nefariousness : NormalMonsterCard
{
    public Archfiend_Marmot_of_Nefariousness()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 4,
            baseDef: 6,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }
}
