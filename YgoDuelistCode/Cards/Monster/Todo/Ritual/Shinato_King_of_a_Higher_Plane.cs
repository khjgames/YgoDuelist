using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Shinato_King_of_a_Higher_Plane : RitualMonsterCard
{
    public Shinato_King_of_a_Higher_Plane()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 33,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

}
