using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Zombie_Warrior : FusionMonsterCard
{
    public Zombie_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 12,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie,
            fusionMaterialTypes: new[]
            {
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Skull_Servant),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Battle_Warrior)
            },
            duelMonsterDefensePlayEnergyOverride: 0,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }
}
