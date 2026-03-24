using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class The_Masked_Beast : RitualMonsterCard
{
    public The_Masked_Beast()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 32,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

}
