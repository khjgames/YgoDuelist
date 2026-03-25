using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class St_Joan : FusionMonsterCard
{
    public St_Joan()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.The_Forgiving_Maiden),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Darklord_Marie))
    {
    }
}
