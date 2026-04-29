using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.TrapMonster;

/// <summary>Trap Monster form of <see cref="Embodiment_of_Apophis"/> (only Special Summoned by that trap).</summary>
public sealed class Embodiment_of_Apophis_Trap_Monster : NormalMonsterCard
{
    public Embodiment_of_Apophis_Trap_Monster()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 16,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Reptile)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.None;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => true;

    public override string PortraitPath => ModelDb.Card<Embodiment_of_Apophis>().PortraitPath;

    public override string CustomPortraitPath => ModelDb.Card<Embodiment_of_Apophis>().CustomPortraitPath;
}
