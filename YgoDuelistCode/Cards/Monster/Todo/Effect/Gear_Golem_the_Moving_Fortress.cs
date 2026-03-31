using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Gear_Golem_the_Moving_Fortress : EffectMonsterCard
{
    public Gear_Golem_the_Moving_Fortress()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 8,
            baseDef: 22,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

}
