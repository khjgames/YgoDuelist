using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Slifer_the_Sky_Dragon : EffectMonsterCard
{
    public Slifer_the_Sky_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: -1,
            baseDef: -1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.DivineBeast)
    {
    }

}
