using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared logic for pack/shop weighting: <see cref="YgoDuelistCard.RelatedCards"/> plus fusion products that use the card as a named material.
/// </summary>
internal static class YgoRelatedCardWeighting
{
    internal static void AccumulateRelatedWeight(YgoDuelistCard ygo, int delta, Dictionary<ModelId, int> target)
    {
        void AddType(Type t)
        {
            try
            {
                CardModel related = YgoPackCardCatalog.CardFromType(t);
                target[related.Id] = target.GetValueOrDefault(related.Id, 0) + delta;
            }
            catch
            {
            }
        }

        var seen = new HashSet<Type>();
        foreach (Type t in ygo.RelatedCards)
        {
            seen.Add(t);
            AddType(t);
        }

        if (ygo is AbstractMonsterCard)
        {
            foreach (Type t in FusionMaterialArchetypeIndex.GetFusionProductsUsingMaterial(ygo.GetType()))
            {
                if (seen.Add(t))
                    AddType(t);
            }
        }
    }
}
