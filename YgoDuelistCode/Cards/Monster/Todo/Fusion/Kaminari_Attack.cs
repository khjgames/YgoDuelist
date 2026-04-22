using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Kaminari_Attack : FusionMonsterCard
{
    public override int AttackPortionCount => 2;
    public override bool BulkBundled => true;

    public Kaminari_Attack()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 19,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Thunder,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Ocubeam),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Mega_Thunderball))
    {
    }

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 0.92f * 0.7f;
    
}
