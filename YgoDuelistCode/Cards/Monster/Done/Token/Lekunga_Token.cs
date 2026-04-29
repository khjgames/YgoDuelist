using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;

public sealed class Lekunga_Token : YgoTokenNormalMonster
{
    public Lekunga_Token()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 7,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }
}
