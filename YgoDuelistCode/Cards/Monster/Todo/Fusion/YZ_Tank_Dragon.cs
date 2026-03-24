using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class YZ_Tank_Dragon : FusionMonsterCard
{
    public YZ_Tank_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 21,
            baseDef: 22,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Y_Dragon_Head),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Z_Metal_Tank))
    {
    }

}
