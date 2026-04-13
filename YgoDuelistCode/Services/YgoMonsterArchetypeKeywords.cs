using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YGO-style archetype tags as <see cref="CardKeyword"/>s (see <c>card_keywords.json</c>).
/// Register monster types here so they get the keyword on the card, in command menus, and in spell checks.
/// </summary>
public static class YgoMonsterArchetypeKeywords
{
    /// <summary>Hover title: <c>Dark Magician</c>; use for relics / effects: <c>Keywords.Contains</c>.</summary>
    public static readonly CardKeyword DarkMagicianArchetypeKeyword = (CardKeyword)20054;

    /// <summary>Hover title: <c>Blue-Eyes White Dragon</c>.</summary>
    public static readonly CardKeyword BlueEyesWhiteDragonArchetypeKeyword = (CardKeyword)20055;

    /// <summary>Types that receive <see cref="DarkMagicianArchetypeKeyword"/> (spell previews, tooling).</summary>
    public static IReadOnlyList<Type> DarkMagicianArchetypeMonsterTypes =>
        FilterMonsterTypes(YgoCardArchetypeRegistry.GetTypes(YgoCardArchetype.DarkMagician));

    /// <summary>Types that receive <see cref="BlueEyesWhiteDragonArchetypeKeyword"/>.</summary>
    public static IReadOnlyList<Type> BlueEyesWhiteDragonArchetypeMonsterTypes =>
        FilterMonsterTypes(YgoCardArchetypeRegistry.GetTypes(YgoCardArchetype.BlueEyesWhiteDragon));

    /// <summary>Keywords applied to this monster type for UI and <see cref="CardModel.Keywords"/> checks.</summary>
    public static IEnumerable<CardKeyword> KeywordsForMonsterType(Type monsterType)
    {
        foreach (Type t in YgoCardArchetypeRegistry.GetTypes(YgoCardArchetype.DarkMagician))
        {
            if (t == monsterType && typeof(BaseMonsterCard).IsAssignableFrom(t))
            {
                yield return DarkMagicianArchetypeKeyword;
                yield break;
            }
        }

        foreach (Type t in YgoCardArchetypeRegistry.GetTypes(YgoCardArchetype.BlueEyesWhiteDragon))
        {
            if (t == monsterType && typeof(BaseMonsterCard).IsAssignableFrom(t))
            {
                yield return BlueEyesWhiteDragonArchetypeKeyword;
                yield break;
            }
        }
    }

    private static Type[] FilterMonsterTypes(IReadOnlyList<Type> types)
    {
        var list = new List<Type>();
        foreach (Type t in types)
        {
            if (typeof(BaseMonsterCard).IsAssignableFrom(t))
                list.Add(t);
        }

        return list.ToArray();
    }

    public static bool HasKeyword(BaseMonsterCard? m, CardKeyword archetypeKeyword)
    {
        if (m == null)
            return false;
        _ = m.Keywords;
        return m.Keywords.Contains(archetypeKeyword);
    }

    public static bool IsFaceUpDarkMagicianArchetype(BaseMonsterCard? m) =>
        m is { FaceDown: false } && HasKeyword(m, DarkMagicianArchetypeKeyword);

    public static bool IsFaceUpBlueEyesWhiteDragonArchetype(BaseMonsterCard? m) =>
        m is { FaceDown: false } && HasKeyword(m, BlueEyesWhiteDragonArchetypeKeyword);
}
