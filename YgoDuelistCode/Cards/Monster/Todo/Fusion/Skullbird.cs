using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Skullbird : FusionMonsterCard
{
    public Skullbird()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 19,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Takuhee),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Temple_of_Skulls))
    {
    }
}
