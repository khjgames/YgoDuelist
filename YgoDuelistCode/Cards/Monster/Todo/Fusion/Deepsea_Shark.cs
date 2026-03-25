using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Deepsea_Shark : FusionMonsterCard
{
    public Deepsea_Shark()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 19,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fish,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Bottom_Dweller),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Tongyo))
    {
    }
}
