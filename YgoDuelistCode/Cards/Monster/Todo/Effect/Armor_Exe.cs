using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Armor_Exe : EffectMonsterCard
{
    public Armor_Exe()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 24,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock,
            duelMonsterAttackPlayEnergyOverride: 1)
    {
    }

}
