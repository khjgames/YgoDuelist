using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Special_Summon_YZ_Tank_Dragon : Special_Summon_Union_Fusion_From_Field_Base
{
    protected override bool IsValidSourceMaterial(BaseMonsterCard source) =>
        source.GetType() == Yz_Tank_Dragon.RequiredMaterialTypes[0]
        || source.GetType() == Yz_Tank_Dragon.RequiredMaterialTypes[1];

    protected override bool TryGetSummonData(Player player, out FusionMonsterCard fusionTarget, out List<BaseMonsterCard> materials)
    {
        fusionTarget = null!;
        materials = new List<BaseMonsterCard>();
        if (!Yz_Tank_Dragon.PlayerHasInExtraDeck(player))
            return false;
        if (!Yz_Tank_Dragon.TryGetExactFieldMaterials(player, out materials))
            return false;
        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        fusionTarget = extra?.Cards.OfType<Yz_Tank_Dragon>().FirstOrDefault()!;
        return fusionTarget != null;
    }
}
