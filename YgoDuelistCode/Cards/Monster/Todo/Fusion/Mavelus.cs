using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Mavelus : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Mavelus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 13,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Tyhone),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Wings_of_Wicked_Flame))
    {
    }

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.07f * 0.7f;
}
