using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Special_Summon_XZ_Tank_Cannon : Special_Summon_Union_Fusion_From_Field_Base
{
    protected override bool IsValidSourceMaterial(BaseMonsterCard source) =>
        source.GetType() == Xz_Tank_Cannon.RequiredMaterialTypes[0]
        || source.GetType() == Xz_Tank_Cannon.RequiredMaterialTypes[1];

    protected override bool TryGetSummonData(Player player, out FusionMonsterCard fusionTarget, out List<BaseMonsterCard> materials)
    {
        fusionTarget = null!;
        materials = new List<BaseMonsterCard>();
        if (!Xz_Tank_Cannon.PlayerHasInExtraDeck(player))
            return false;
        if (!Xz_Tank_Cannon.TryGetExactFieldMaterials(player, out materials))
            return false;
        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        fusionTarget = extra?.Cards.OfType<Xz_Tank_Cannon>().FirstOrDefault()!;
        return fusionTarget != null;
    }
}
