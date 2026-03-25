using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Perfectly_Ultimate_Great_Moth : EffectMonsterCard
{
    public Perfectly_Ultimate_Great_Moth()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 35,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }
}
