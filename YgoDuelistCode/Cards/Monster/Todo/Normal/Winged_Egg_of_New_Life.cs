using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;

public sealed class Winged_Egg_of_New_Life : NormalMonsterCard
{
    public Winged_Egg_of_New_Life()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 14,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }
}
