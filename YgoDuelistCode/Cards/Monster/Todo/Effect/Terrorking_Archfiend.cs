using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Terrorking_Archfiend : EffectMonsterCard
{
    public Terrorking_Archfiend()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 3)
    {
    }

    public override int PermanentAtkDeltaOnEnemyExecute => IsUpgraded ? 4 : 3;

    public override int GetDuelMonsterPlayEnergyDiscount() =>
        IsUpgraded && IsAttackBattlePosition ? 1 : 0;

    protected override void OnUpgrade() => base.OnUpgrade();
}
