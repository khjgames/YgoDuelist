using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Special_Summon_XY_Dragon_Cannon : Special_Summon_Union_Fusion_From_Field_Base
{
    protected override string CanonicalUnionFusionPortraitPath => ModelDb.Card<Xy_Dragon_Cannon>().PortraitPath;

    protected override bool IsValidSourceMaterial(BaseMonsterCard source) =>
        source.GetType() == Xy_Dragon_Cannon.RequiredMaterialTypes[0]
        || source.GetType() == Xy_Dragon_Cannon.RequiredMaterialTypes[1];

    protected override bool TryGetSummonData(Player player, out FusionMonsterCard fusionTarget, out List<BaseMonsterCard> materials)
    {
        fusionTarget = null!;
        materials = new List<BaseMonsterCard>();
        if (!Xy_Dragon_Cannon.PlayerHasInExtraDeck(player))
            return false;
        if (!Xy_Dragon_Cannon.TryGetExactFieldMaterials(player, out materials))
            return false;
        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        fusionTarget = extra?.Cards.OfType<Xy_Dragon_Cannon>().FirstOrDefault()!;
        return fusionTarget != null;
    }
}
