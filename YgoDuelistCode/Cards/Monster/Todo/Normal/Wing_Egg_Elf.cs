using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;

public sealed class Wing_Egg_Elf : NormalMonsterCard
{
    public Wing_Egg_Elf()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 5,
            baseDef: 13,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }
}
