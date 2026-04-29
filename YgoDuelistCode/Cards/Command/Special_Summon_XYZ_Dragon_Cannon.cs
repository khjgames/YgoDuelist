using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Special_Summon_XYZ_Dragon_Cannon : Special_Summon_Union_Fusion_From_Field_Base
{
    protected override string CanonicalUnionFusionPortraitPath => ModelDb.Card<Xyz_Dragon_Cannon>().PortraitPath;

    protected override bool IsValidSourceMaterial(BaseMonsterCard source) =>
        source.GetType() == Xyz_Dragon_Cannon.RequiredMaterialTypes[0]
        || source.GetType() == Xyz_Dragon_Cannon.RequiredMaterialTypes[1]
        || source.GetType() == Xyz_Dragon_Cannon.RequiredMaterialTypes[2];

    protected override bool TryGetSummonData(Player player, out List<FusionMonsterCard> fusionTargets, out List<BaseMonsterCard> materials)
    {
        fusionTargets = new List<FusionMonsterCard>();
        materials = new List<BaseMonsterCard>();
        if (!Xyz_Dragon_Cannon.PlayerHasInExtraDeck(player))
            return false;
        if (!Xyz_Dragon_Cannon.TryGetExactFieldMaterials(player, out materials))
            return false;
        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        if (extra == null)
            return false;
        fusionTargets = YgoMpCombatOrder.CardsOrderedForMp(extra.Cards)
            .OfType<Xyz_Dragon_Cannon>()
            .Cast<FusionMonsterCard>()
            .ToList();
        return fusionTargets.Count > 0;
    }
}
