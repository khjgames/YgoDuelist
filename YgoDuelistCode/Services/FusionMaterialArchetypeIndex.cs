using System;
using System.Collections.Generic;
using System.Linq;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Reverse index of named fusion materials: built from every concrete <see cref="FusionMonsterCard"/> recipe.
/// Used for pack tags and related-card weighting without duplicating data on each material card.
/// </summary>
public static class FusionMaterialArchetypeIndex
{
    private static readonly object Gate = new();
    private static bool sBuilt;
    private static HashSet<Type> sMaterialTypes = null!;
    private static Dictionary<Type, Type[]> sFusionProductsByMaterial = null!;

    /// <summary>True if this monster type appears as a <see cref="FusionMaterialSlot.NamedType"/> on any fusion.</summary>
    public static bool IsNamedFusionMaterial(Type monsterCardType)
    {
        EnsureBuilt();
        return sMaterialTypes.Contains(monsterCardType);
    }

    /// <summary>Fusion monster types that name <paramref name="monsterCardType"/> as a material (distinct, discovery order).</summary>
    public static IReadOnlyList<Type> GetFusionProductsUsingMaterial(Type monsterCardType)
    {
        EnsureBuilt();
        return sFusionProductsByMaterial.TryGetValue(monsterCardType, out Type[]? arr)
            ? arr
            : Array.Empty<Type>();
    }

    private static void EnsureBuilt()
    {
        lock (Gate)
        {
            if (sBuilt)
                return;
            Build();
            sBuilt = true;
        }
    }

    private static void Build()
    {
        var materials = new HashSet<Type>();
        var productLists = new Dictionary<Type, List<Type>>();

        foreach (Type t in typeof(FusionMonsterCard).Assembly.GetTypes())
        {
            if (t.IsAbstract || !typeof(FusionMonsterCard).IsAssignableFrom(t))
                continue;

            FusionMonsterCard fusion;
            try
            {
                fusion = (FusionMonsterCard)Activator.CreateInstance(t)!;
            }
            catch
            {
                continue;
            }

            foreach (FusionMaterialSlot slot in fusion.FusionMaterialSlots)
            {
                Type? named = slot.NamedType;
                if (named == null)
                    continue;
                if (!typeof(BaseMonsterCard).IsAssignableFrom(named) || named.IsAbstract)
                    continue;

                materials.Add(named);
                if (!productLists.TryGetValue(named, out List<Type>? list))
                {
                    list = new List<Type>();
                    productLists[named] = list;
                }

                if (!list.Contains(t))
                    list.Add(t);
            }
        }

        sMaterialTypes = materials;
        sFusionProductsByMaterial = productLists.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }
}
