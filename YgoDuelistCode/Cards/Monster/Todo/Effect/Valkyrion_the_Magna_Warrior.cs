using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Valkyrion_the_Magna_Warrior : EffectMonsterCard
{
    public Valkyrion_the_Magna_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 35,
            baseDef: 38,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }
}
