using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Special_Summon_YZ_Tank_Dragon : Special_Summon_Union_Fusion_From_Field_Base
{
    protected override string CanonicalUnionFusionPortraitPath => ModelDb.Card<Yz_Tank_Dragon>().PortraitPath;

    protected override bool IsValidSourceMaterial(BaseMonsterCard source) =>
        source.GetType() == Yz_Tank_Dragon.RequiredMaterialTypes[0]
        || source.GetType() == Yz_Tank_Dragon.RequiredMaterialTypes[1];

    protected override bool TryGetSummonData(Player player, out List<FusionMonsterCard> fusionTargets, out List<BaseMonsterCard> materials)
    {
        fusionTargets = new List<FusionMonsterCard>();
        materials = new List<BaseMonsterCard>();
        if (!Yz_Tank_Dragon.PlayerHasInExtraDeck(player))
            return false;
        if (!Yz_Tank_Dragon.TryGetExactFieldMaterials(player, out materials))
            return false;
        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        if (extra == null)
            return false;
        fusionTargets = YgoMpCombatOrder.CardsOrderedForMp(extra.Cards)
            .OfType<Yz_Tank_Dragon>()
            .Cast<FusionMonsterCard>()
            .ToList();
        return fusionTargets.Count > 0;
    }
}
