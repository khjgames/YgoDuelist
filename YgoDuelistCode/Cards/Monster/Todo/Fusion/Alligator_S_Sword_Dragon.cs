using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Alligator_S_Sword_Dragon : FusionMonsterCard
{
    public Alligator_S_Sword_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 17,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Baby_Dragon),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Alligator_S_Sword))
    {
    }

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;
}
