using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YGO-style archetype tags as <see cref="CardKeyword"/>s (see <c>card_keywords.json</c>).
/// Which concrete monster types belong to an archetype for keywords and related-card tooling is defined in
/// <see cref="YgoCardArchetypeRegistry"/> (<c>s_blueEyes</c>, <c>s_darkMagician</c>, …).
/// </summary>
public static class YgoMonsterArchetypeKeywords
{
    /// <summary>Hover title: <c>Dark Magician</c>; use for relics / effects: <c>Keywords.Contains</c>.</summary>
    public static readonly CardKeyword DarkMagicianArchetypeKeyword = (CardKeyword)20054;

    /// <summary>Hover title: <c>Blue-Eyes White Dragon</c>.</summary>
    public static readonly CardKeyword BlueEyesWhiteDragonArchetypeKeyword = (CardKeyword)20055;

    /// <summary>Types that receive <see cref="DarkMagicianArchetypeKeyword"/> (spell previews, tooling).</summary>
    public static IReadOnlyList<Type> DarkMagicianArchetypeMonsterTypes =>
        FilterMonsterTypes(YgoCardArchetypeRegistry.GetStrictArchetypeTypes(YgoCardArchetype.DarkMagician));

    /// <summary>Types that receive <see cref="BlueEyesWhiteDragonArchetypeKeyword"/>.</summary>
    public static IReadOnlyList<Type> BlueEyesWhiteDragonArchetypeMonsterTypes =>
        FilterMonsterTypes(YgoCardArchetypeRegistry.GetStrictArchetypeTypes(YgoCardArchetype.BlueEyesWhiteDragon));

    /// <summary>Keywords applied to this monster type for UI and <see cref="CardModel.Keywords"/> checks.</summary>
    public static IEnumerable<CardKeyword> KeywordsForMonsterType(Type monsterType)
    {
        foreach (Type t in YgoCardArchetypeRegistry.GetStrictArchetypeTypes(YgoCardArchetype.DarkMagician))
        {
            if (t == monsterType && typeof(BaseMonsterCard).IsAssignableFrom(t))
            {
                yield return DarkMagicianArchetypeKeyword;
                yield break;
            }
        }

        foreach (Type t in YgoCardArchetypeRegistry.GetStrictArchetypeTypes(YgoCardArchetype.BlueEyesWhiteDragon))
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

    /// <summary>
    /// Spell / trap availability: true when the owner has a <b>living</b> duel monster pet whose source field card is a
    /// face-up member of the archetype (keyword from <see cref="YgoCardArchetypeRegistry"/>). Uses the pet list instead of
    /// <see cref="DuelMonsterFieldRegistry.OrderedFieldMonsters"/> alone so playability cannot drift from an actual summon.
    /// </summary>
    public static bool PlayerControlsFaceUpBlueEyesArchetypeMonster(Player? owner)
    {
        if (owner?.PlayerCombatState == null)
            return false;
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            BaseMonsterCard? m = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
            if (IsFaceUpBlueEyesWhiteDragonArchetype(m))
                return true;
        }

        return false;
    }

    /// <inheritdoc cref="PlayerControlsFaceUpBlueEyesArchetypeMonster"/>
    public static bool PlayerControlsFaceUpDarkMagicianArchetypeMonster(Player? owner)
    {
        if (owner?.PlayerCombatState == null)
            return false;
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            BaseMonsterCard? m = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
            if (IsFaceUpDarkMagicianArchetype(m))
                return true;
        }

        return false;
    }
}
