using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;

public sealed class Army_Ant_Token : YgoTokenNormalMonster
{
    public Army_Ant_Token()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 5,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }
}
