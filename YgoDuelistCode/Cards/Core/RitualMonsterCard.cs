using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class RitualMonsterCard : EffectMonsterCard
{
    protected RitualMonsterCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace = DuelMonsterRace.Warrior,
        int? duelMonsterAttackPlayEnergyOverride = null,
        int? duelMonsterDefensePlayEnergyOverride = null)
        : base(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace,
            duelMonsterAttackPlayEnergyOverride, duelMonsterDefensePlayEnergyOverride)
    {
    }

    protected override int MonsterConduitStarCost => 0;

    public override YgoCardType YgoCardType => YgoCardType.RitualMonster;

    /// <summary>Ritual monsters are only summoned via ritual spells, not played from the hand.</summary>
    protected override bool IsPlayable
    {
        get
        {
            if (CombatManager.Instance?.IsInProgress == true && Pile?.Type == PileType.Hand)
                return false;
            return base.IsPlayable;
        }
    }
}
