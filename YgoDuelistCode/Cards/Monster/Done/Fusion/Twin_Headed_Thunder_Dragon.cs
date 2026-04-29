using MegaCrit.Sts2.Core.Entities.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Twin_Headed_Thunder_Dragon : FusionMonsterCard
{
    public override int AttackPortionCount => 2;

    public Twin_Headed_Thunder_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Thunder,
            typeof(Thunder_Dragon),
            typeof(Thunder_Dragon))
    {
    }

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.05f;

    public override Type[] BundledCards => new[] { typeof(Thunder_Dragon) };
}
