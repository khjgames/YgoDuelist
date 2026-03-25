using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Bracchio_Raidus : FusionMonsterCard
{
    public Bracchio_Raidus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 22,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dinosaur,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Two_Headed_King_Rex),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Crawling_Dragon_2))
    {
    }
}
