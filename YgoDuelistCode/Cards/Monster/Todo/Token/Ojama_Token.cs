using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

public sealed class Ojama_Token : YgoTokenNormalMonster
{
    public Ojama_Token()
        : base(
            cost: 0,
            type: CardType.Skill,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 0,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }
}
