using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Ojama_King : FusionMonsterCard
{
    public Ojama_King()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 0,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast,
            fusionMaterialTypes: new[]
            {
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Ojama_Green),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Ojama_Yellow),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Ojama_Black)
            },
            duelMonsterDefensePlayEnergyOverride: 2
            )
    {
    }

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.13f;
    
}
