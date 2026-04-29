using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Vermillion_Sparrow : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Vermillion_Sparrow()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 19,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Pyro,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Rhaimundos_of_the_Red_Sword),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Fireyarou))
    {
    }
    
    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 0.96f * 0.7f;

}
