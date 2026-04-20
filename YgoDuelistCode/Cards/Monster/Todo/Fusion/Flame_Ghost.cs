using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Flame_Ghost : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Flame_Ghost()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 10,
            baseDef: 11,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie,
            fusionMaterialTypes: new[]
            {
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Skull_Servant),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Dissolverock)
            }
            )
    {
    }
        
    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.10f * 0.7f;

}
