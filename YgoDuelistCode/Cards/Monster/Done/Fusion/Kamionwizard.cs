using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Kamionwizard : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Kamionwizard()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 13,
            baseDef: 11,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Mystical_Elf),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Curtain_of_the_Dark_Ones))
    {
    }
    
    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.10f * 0.7f;

}
