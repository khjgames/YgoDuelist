using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Yamata_Dragon : EffectMonsterCard
{
    public Yamata_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 26,
            baseDef: 31,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

}
