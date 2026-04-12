using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

public sealed class Insect_Monster_Token : YgoTokenNormalMonster
{
    public Insect_Monster_Token()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 1,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }
}
