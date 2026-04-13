using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// Builds <see cref="YgoDuelistCard.RelatedCards"/> from explicit extras, archetypes, fusion links, rituals, and double-tribute pairings.
/// </summary>
public static class YgoRelatedCardsComposer
{
    /// <param name="fusionSeed">For <see cref="FusionMonsterCard"/>: fusion monster plus named materials (see <c>_fusionRelatedCards</c>).</param>
    /// <param name="explicitAdditional">Card-specific related types on top of rules below.</param>
    public static Type[] Compose(YgoDuelistCard card, Type[]? fusionSeed, params Type[]? explicitAdditional)
    {
        var set = new HashSet<Type>();

        void Add(Type? t)
        {
            if (t == null)
                return;
            if (!typeof(CardModel).IsAssignableFrom(t) || t.IsAbstract)
                return;
            set.Add(t);
        }

        Add(card.GetType());

        if (fusionSeed != null)
        {
            foreach (Type t in fusionSeed)
                Add(t);
        }

        if (explicitAdditional != null)
        {
            foreach (Type t in explicitAdditional)
                Add(t);
        }

        YgoCardArchetype flags = card.CardArchetypes | YgoCardArchetypeRegistry.GetImplicitArchetypes(card.GetType());
        foreach (YgoCardArchetype bit in Enum.GetValues<YgoCardArchetype>())
        {
            if (bit == YgoCardArchetype.None)
                continue;
            if ((flags & bit) == 0)
                continue;
            foreach (Type t in YgoCardArchetypeRegistry.GetTypes(bit))
                Add(t);
        }

        if (card is BaseMonsterCard bm)
        {
            Type host = bm.GetType();
            foreach (Type t in FusionMaterialArchetypeIndex.GetFusionProductsUsingMaterial(host))
                Add(t);

            if (ShouldConsiderDoubleTributeTargets(bm))
            {
                foreach (Type t in DoubleTributeMaterialCatalog.EnumerateMaterialTypesMatchingSummonTarget(bm))
                    Add(t);
            }
        }

        if (card is RitualMonsterCard)
        {
            Type? spell = RitualArchetypeMeta.PairedRitualSpellType(card.GetType());
            Add(spell);
            foreach (Type t in RitualArchetypeMeta.NonRitualCardsReferencingRitualInLocalization)
                Add(t);
        }

        if (card is RitualSpellCard rs)
        {
            Type target = rs.RitualTargetMonsterType;
            if (target.IsSubclassOf(typeof(RitualMonsterCard)) && !target.IsAbstract)
                Add(target);
            foreach (Type t in RitualArchetypeMeta.NonRitualCardsReferencingRitualInLocalization)
                Add(t);
        }

        var list = new List<Type>(set);
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return list.ToArray();
    }

    private static bool ShouldConsiderDoubleTributeTargets(BaseMonsterCard bm)
    {
        if (bm.YgoCardType == YgoCardType.FusionMonster || bm.YgoCardType == YgoCardType.RitualMonster)
            return false;
        if (bm.YgoCardType != YgoCardType.Monster && bm.YgoCardType != YgoCardType.EffectMonster)
            return false;
        return bm.GetEffectiveDuelMonsterLevel() >= 7;
    }
}
