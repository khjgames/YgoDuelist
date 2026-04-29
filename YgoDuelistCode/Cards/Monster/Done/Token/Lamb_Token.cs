using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;

public sealed class Lamb_Token : YgoTokenNormalMonster
{
    public Lamb_Token()
        : base(
            cost: 0,
            type: CardType.Skill,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override int TokenAlternatePortraitCount => 2;
}
