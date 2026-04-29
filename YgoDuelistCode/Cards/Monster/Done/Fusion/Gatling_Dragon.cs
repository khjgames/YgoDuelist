using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Gatling_Dragon : FusionMonsterCard
{
    public override int AttackPortionCount => 5;

    public Gatling_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 26,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Barrel_Dragon),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Blowback_Dragon))
    {
    }

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.13f;
}
