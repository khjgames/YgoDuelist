using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Rabid_Horseman : FusionMonsterCard
{
    public Rabid_Horseman()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 20,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Battle_Ox),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Mystic_Horseman))
    {
    }
}
