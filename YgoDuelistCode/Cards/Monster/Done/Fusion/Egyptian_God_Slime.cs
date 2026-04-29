using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Egyptian_God_Slime : FusionMonsterCard
{
    public Egyptian_God_Slime()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 30,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua,
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.AquaRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.Level10WaterOnly()))
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.God;

    public static bool PlayerHasSlimeInExtraDeck(Player player)
    {
        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        return extra != null && extra.Cards.Any(static c => c is Egyptian_God_Slime);
    }

    public static bool QualifiesAsSlimeTributeMaterial(BaseMonsterCard m) =>
        m.DuelMonsterLevel == 10
        && m.DuelMonsterRace == DuelMonsterRace.Aqua
        && m.BaseAtk == 0;
}
