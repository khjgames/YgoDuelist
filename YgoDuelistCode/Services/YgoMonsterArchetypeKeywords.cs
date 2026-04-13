using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

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

    private static readonly Type[] s_darkMagicianTypes =
    [
        typeof(Dark_Magician),
        typeof(Dark_Magician_Girl),
        typeof(Skilled_Dark_Magician),
        typeof(Dark_Magician_of_Chaos),
        typeof(Toon_Dark_Magician_Girl),
        typeof(Magician_of_Black_Chaos),
        typeof(Dark_Paladin),
        typeof(Dark_Flare_Knight),
        typeof(Dark_Sage),
    ];

    private static readonly Type[] s_blueEyesTypes =
    [
        typeof(Blue_Eyes_White_Dragon),
        typeof(Blue_Eyes_Ultimate_Dragon),
        typeof(Blue_Eyes_Toon_Dragon),
        typeof(Paladin_of_White_Dragon),
        typeof(Dragon_Master_Knight),
    ];

    /// <summary>Types that receive <see cref="DarkMagicianArchetypeKeyword"/> (spell previews, tooling).</summary>
    public static IReadOnlyList<Type> DarkMagicianArchetypeMonsterTypes => s_darkMagicianTypes;

    /// <summary>Types that receive <see cref="BlueEyesWhiteDragonArchetypeKeyword"/>.</summary>
    public static IReadOnlyList<Type> BlueEyesWhiteDragonArchetypeMonsterTypes => s_blueEyesTypes;

    /// <summary>Keywords applied to this monster type for UI and <see cref="CardModel.Keywords"/> checks.</summary>
    public static IEnumerable<CardKeyword> KeywordsForMonsterType(Type monsterType)
    {
        foreach (Type t in s_darkMagicianTypes)
        {
            if (t == monsterType)
            {
                yield return DarkMagicianArchetypeKeyword;
                yield break;
            }
        }

        foreach (Type t in s_blueEyesTypes)
        {
            if (t == monsterType)
            {
                yield return BlueEyesWhiteDragonArchetypeKeyword;
                yield break;
            }
        }
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
