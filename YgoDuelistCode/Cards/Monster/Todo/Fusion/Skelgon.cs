using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Skelgon : FusionMonsterCard
{
    public override int AttackPortionCount => 2;
    public override bool BulkBundled => true;

    public Skelgon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 17,
            baseDef: 19,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.The_Snake_Hair),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Blackland_Fire_Dragon))
    {
    }
    
    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 0.96f * 0.7f;

}
