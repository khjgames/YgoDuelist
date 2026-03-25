using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Dragon_Master_Knight : FusionMonsterCard
{
    public Dragon_Master_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 12,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 50,
            baseDef: 50,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual.Black_Luster_Soldier),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion.Blue_Eyes_Ultimate_Dragon))
    {
    }
}
