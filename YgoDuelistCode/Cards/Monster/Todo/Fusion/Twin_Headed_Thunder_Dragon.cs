using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Twin_Headed_Thunder_Dragon : FusionMonsterCard
{
    public Twin_Headed_Thunder_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Thunder,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Thunder_Dragon),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Thunder_Dragon))
    {
    }

}
