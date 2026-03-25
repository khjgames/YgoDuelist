using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>1-cost; when this card executes a monster, permanently lose 2 ATK (1 if upgraded).</summary>
public sealed class Zombyra_the_Dark : EffectMonsterCard
{
    public Zombyra_the_Dark()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 21,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            duelMonsterAttackPlayEnergyOverride: 1)
    {
    }

    public override int PermanentAtkDeltaOnEnemyExecute => IsUpgraded ? -1 : -2;

    public override bool AppliesPermanentAtkDeltaOnEnemyKill(Creature killedEnemy) => killedEnemy.IsMonster;

    protected override void OnUpgrade() => base.OnUpgrade();
}
