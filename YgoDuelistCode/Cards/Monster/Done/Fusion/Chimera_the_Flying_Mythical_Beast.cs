using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Chimera_the_Flying_Mythical_Beast : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public override int AttackPortionCount => 2;

    public Chimera_the_Flying_Mythical_Beast()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 21,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Gazelle_the_King_of_Mythical_Beasts),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Berfomet))
    {
    }

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.00f * 0.7f;

}
