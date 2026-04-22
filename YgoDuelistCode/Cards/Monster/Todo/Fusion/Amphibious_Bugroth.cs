using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Amphibious_Bugroth : FusionMonsterCard
{
    public override int AttackPortionCount => 2;
    public override bool BulkBundled => true;

    public Amphibious_Bugroth()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 18,
            baseDef: 13,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Ground_Attacker_Bugroth),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Sentinel_of_the_Seas))
    {
    }
    
    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.02f * 0.7f;

}
