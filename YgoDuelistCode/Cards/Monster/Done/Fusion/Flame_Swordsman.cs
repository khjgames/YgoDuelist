using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Flame_Swordsman : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Flame_Swordsman()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 18,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Flame_Manipulator),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Masaki_the_Legendary_Swordsman))
    {
    }
    
    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.05f * 0.7f;

}
