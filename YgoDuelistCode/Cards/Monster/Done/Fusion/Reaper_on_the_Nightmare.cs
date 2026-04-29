using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Reaper_on_the_Nightmare : FusionMonsterCard
{
    public Reaper_on_the_Nightmare()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 9,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie,
            fusionMaterialTypes: new[]
            {
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Spirit_Reaper),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Nightmare_Horse)
            }
            )
    {
    }

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.10f;

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

    public override bool AttackDealsFullBlightedDamage => true;
}
