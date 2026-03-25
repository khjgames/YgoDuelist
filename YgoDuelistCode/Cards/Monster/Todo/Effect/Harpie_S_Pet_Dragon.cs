using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Harpie_S_Pet_Dragon : EffectMonsterCard
{
    public Harpie_S_Pet_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 20,
            baseDef: 25,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }
}
