using System;
using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YGO cards eligible for tag-based packs: <c>(<see cref="GetEffectivePackTags"/> &amp; mask) != 0</c> (inclusive OR on pack themes).
/// <see cref="YgoCardPackTags.Starter"/> is not a rolled pack category but remains on cards for other systems.
/// </summary>
public static class YgoPackCardCatalog
{
    /// <summary>
    /// Declared <see cref="YgoDuelistCard.PackTags"/> plus implicit tags for monsters that are named fusion materials
    /// elsewhere (<see cref="FusionMaterialArchetypeIndex"/>): Fusion, attribute/race profile, and Normal/Ritual subtype when applicable.
    /// </summary>
    public static YgoCardPackTags GetEffectivePackTags(YgoDuelistCard y)
    {
        YgoCardPackTags tags = y.PackTags;
        Type t = y.GetType();
        if (!FusionMaterialArchetypeIndex.IsNamedFusionMaterial(t))
            return tags;

        tags |= YgoCardPackTags.Fusion;
        if (y is AbstractMonsterCard m)
        {
            tags |= FusionMonsterCard.PackTagsForFusionProfile(m.DuelMonsterAttribute, m.DuelMonsterRace);
            if (m.YgoCardType == YgoCardType.Monster)
                tags |= YgoCardPackTags.Normal;
            if (m.YgoCardType == YgoCardType.RitualMonster)
                tags |= YgoCardPackTags.Ritual;
        }

        return tags;
    }

    private static readonly object Gate = new();
    private static List<CardModel>? sAllYgoTemplates;
    private static MethodInfo? sModelDbCardNoArg;

    /// <summary>Main pool for rolling pack themes (no Starter, Bundled, None, WinCon, God — subtags added separately).</summary>
    public static readonly YgoCardPackTags[] PackThemeMainTags =
    {
        YgoCardPackTags.Earth, YgoCardPackTags.Water, YgoCardPackTags.Wind, YgoCardPackTags.Fire,
        YgoCardPackTags.Dark, YgoCardPackTags.Light, YgoCardPackTags.Fusion, YgoCardPackTags.Ritual,
        YgoCardPackTags.Ocean, YgoCardPackTags.Insect, YgoCardPackTags.Machine, YgoCardPackTags.Dragon,
        YgoCardPackTags.Zombie, YgoCardPackTags.Fiend, YgoCardPackTags.Spellcaster, YgoCardPackTags.Warrior,
        YgoCardPackTags.Heal, YgoCardPackTags.Draw, YgoCardPackTags.Chance, YgoCardPackTags.Burn,
        YgoCardPackTags.Normal, YgoCardPackTags.Spell, YgoCardPackTags.Trap
    };

    public static readonly YgoCardPackTags[] PackThemeSubTags =
    {
        YgoCardPackTags.Banish, YgoCardPackTags.WinCon, YgoCardPackTags.God
    };

    public static IReadOnlyList<CardModel> GetAllYgoTemplates()
    {
        lock (Gate)
        {
            if (sAllYgoTemplates != null)
                return sAllYgoTemplates;

            var list = new List<CardModel>();
            foreach (Type t in typeof(YgoDuelistCard).Assembly.GetTypes())
            {
                if (t.IsAbstract || !t.IsSubclassOf(typeof(YgoDuelistCard)))
                    continue;

                CardModel model;
                try
                {
                    model = CardFromType(t);
                }
                catch
                {
                    continue;
                }

                if (model is YgoDuelistCard ygo && GetEffectivePackTags(ygo) != YgoCardPackTags.None)
                    list.Add(model);
            }

            sAllYgoTemplates = list;
            return sAllYgoTemplates;
        }
    }

    /// <summary>Unlocked YGO cards whose effective pack tags (declared + fusion-material inference) intersect <paramref name="tagMask"/>.</summary>
    public static List<CardModel> GetUnlockedPool(Player player, YgoCardPackTags tagMask)
    {
        HashSet<ModelId> unlocked = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(c => c.Id)
            .ToHashSet();

        return GetAllYgoTemplates()
            .Where(c => unlocked.Contains(c.Id) && c is YgoDuelistCard y && (GetEffectivePackTags(y) & tagMask) != 0)
            .ToList();
    }

    internal static CardModel CardFromType(Type cardType)
    {
        sModelDbCardNoArg ??= typeof(ModelDb)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(ModelDb.Card) && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 0);

        MethodInfo closed = sModelDbCardNoArg.MakeGenericMethod(cardType);
        return (CardModel)closed.Invoke(null, null)!;
    }
}
