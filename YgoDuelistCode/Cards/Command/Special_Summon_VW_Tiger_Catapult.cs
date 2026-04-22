using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Special_Summon_VW_Tiger_Catapult : Special_Summon_Union_Fusion_From_Field_Base
{
    protected override string CanonicalUnionFusionPortraitPath => ModelDb.Card<Vw_Tiger_Catapult>().PortraitPath;

    protected override bool IsValidSourceMaterial(BaseMonsterCard source) =>
        source.GetType() == Vw_Tiger_Catapult.RequiredMaterialTypes[0]
        || source.GetType() == Vw_Tiger_Catapult.RequiredMaterialTypes[1];

    protected override bool TryGetSummonData(Player player, out FusionMonsterCard fusionTarget, out List<BaseMonsterCard> materials)
    {
        fusionTarget = null!;
        materials = new List<BaseMonsterCard>();
        if (!Vw_Tiger_Catapult.PlayerHasInExtraDeck(player))
            return false;
        if (!Vw_Tiger_Catapult.TryGetExactFieldMaterials(player, out materials))
            return false;
        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        fusionTarget = extra?.Cards.OfType<Vw_Tiger_Catapult>().FirstOrDefault()!;
        return fusionTarget != null;
    }
}
