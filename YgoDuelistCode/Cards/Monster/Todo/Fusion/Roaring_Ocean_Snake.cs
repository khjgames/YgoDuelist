using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Roaring_Ocean_Snake : FusionMonsterCard
{
    public Roaring_Ocean_Snake()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 21,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Mystic_Lamp),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Hyosube))
    {
    }
}
