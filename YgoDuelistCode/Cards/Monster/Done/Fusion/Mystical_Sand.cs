using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Mystical_Sand : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Mystical_Sand()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 21,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Giant_Soldier_of_Stone),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Ancient_Elf))
    {
    }
    
    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 0.97f * 0.7f;

}
