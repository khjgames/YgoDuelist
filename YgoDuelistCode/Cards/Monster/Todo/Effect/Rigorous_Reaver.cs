using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Rigorous_Reaver : EffectMonsterCard
{
    public Rigorous_Reaver()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 16,
            baseDef: 100,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }
}
