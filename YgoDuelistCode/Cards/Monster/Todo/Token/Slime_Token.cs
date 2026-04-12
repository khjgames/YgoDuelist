using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

public sealed class Slime_Token : YgoTokenNormalMonster
{
    public Slime_Token()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 5,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }
}
