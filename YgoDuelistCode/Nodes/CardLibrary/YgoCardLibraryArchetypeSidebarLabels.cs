using System;
using System.Collections.Generic;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

internal static class YgoCardLibraryArchetypeSidebarLabels
{
    private static readonly YgoCardArchetype[] s_priorityOrder =
    [
        YgoCardArchetype.SpiritMonster,
        YgoCardArchetype.BlueEyesWhiteDragon,
        YgoCardArchetype.DarkMagician,
        YgoCardArchetype.HarpieLady,
        YgoCardArchetype.RedEyesBlackDragon,
        YgoCardArchetype.Gravekeeper,
        YgoCardArchetype.Umi,
        YgoCardArchetype.ZombieBoost,
        YgoCardArchetype.DivineBeast,
        YgoCardArchetype.GenericDoubleSummoner,
    ];

    public static IEnumerable<YgoCardArchetype> SidebarOrder()
    {
        var seen = new HashSet<YgoCardArchetype>();
        foreach (YgoCardArchetype a in s_priorityOrder)
        {
            if (a == YgoCardArchetype.None)
                continue;
            if (seen.Add(a))
                yield return a;
        }

        foreach (YgoCardArchetype a in Enum.GetValues<YgoCardArchetype>())
        {
            if (a == YgoCardArchetype.None)
                continue;
            if (seen.Add(a))
                yield return a;
        }
    }

    public static string TickboxLabel(YgoCardArchetype archetype) =>
        archetype switch
        {
            YgoCardArchetype.SpiritMonster => "Spirit Monster",
            YgoCardArchetype.BlueEyesWhiteDragon => "Blue-Eyes",
            YgoCardArchetype.DarkMagician => "Dark Magician",
            YgoCardArchetype.HarpieLady => "Harpie Lady",
            YgoCardArchetype.RedEyesBlackDragon => "Red-Eyes",
            YgoCardArchetype.DivineBeast => "Divine Beast",
            YgoCardArchetype.GenericDoubleSummoner => "Double Tribute",
            YgoCardArchetype.GenericAllMonstersTempStatBoost => "All Mon. Temp Boost",
            YgoCardArchetype.GenericAllMonstersContinuousStatBoost => "All Mon. Cont. Boost",
            YgoCardArchetype.GenericSingleMonsterTempStatBoost => "Single Mon. Temp Boost",
            _ => archetype.ToString(),
        };
}
