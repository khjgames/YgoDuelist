using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Five_Headed_Dragon : FusionMonsterCard
{
    public override int AttackPortionCount => 5;
    /// <summary>YGO: 5 Dragon monsters — requirement-based slots (any Dragon normal/effect that satisfies race).</summary>
    public Five_Headed_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 12,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 50,
            baseDef: 50,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()))
    {
    }
}
