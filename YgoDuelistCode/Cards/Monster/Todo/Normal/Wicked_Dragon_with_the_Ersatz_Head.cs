using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;

public sealed class Wicked_Dragon_with_the_Ersatz_Head : NormalMonsterCard
{
    public Wicked_Dragon_with_the_Ersatz_Head()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 9,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }
}
