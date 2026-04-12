using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// After vanilla compendium ordering, optionally re-order by printed ATK/DEF within each locked / unlocked bucket.
/// </summary>
[HarmonyPatch(typeof(NCardGrid), nameof(NCardGrid.SetCards), typeof(IReadOnlyList<CardModel>), typeof(PileType),
    typeof(List<SortingOrders>), typeof(Task))]
public static class YgoCardLibraryGridStatSortPatch
{
    static readonly FieldInfo CardsField =
        AccessTools.Field(typeof(NCardGrid), "_cards")
        ?? throw new InvalidOperationException("NCardGrid._cards not found.");

    static readonly MethodInfo GetCardVisibilityMethod =
        AccessTools.DeclaredMethod(typeof(NCardGrid), "GetCardVisibility", [typeof(CardModel)])
        ?? throw new InvalidOperationException("NCardGrid.GetCardVisibility(CardModel) not found.");

    static void Postfix(NCardGrid __instance)
    {
        if (__instance is not NCardLibraryGrid)
            return;

        NCardLibrary? library = FindParentCardLibrary(__instance);
        if (library == null
            || !YgoCardLibrarySidebarFilterRegistry.States.TryGetValue(library, out YgoCardLibrarySidebarFilterState? state))
            return;

        if (state.PrimaryMonsterStatSort == YgoCardLibraryMonsterStatSortAxis.None)
            return;

        var cards = (List<CardModel>)CardsField.GetValue(__instance)!;
        if (cards.Count <= 1)
            return;

        bool descending = state.PrimaryMonsterStatSort switch
        {
            YgoCardLibraryMonsterStatSortAxis.PackWeight => state.PackWeight.SortButton?.IsDescending ?? true,
            YgoCardLibraryMonsterStatSortAxis.Atk => state.Atk.SortButton?.IsDescending ?? true,
            YgoCardLibraryMonsterStatSortAxis.Def => state.Def.SortButton?.IsDescending ?? true,
            _ => true
        };

        bool useAtk = state.PrimaryMonsterStatSort == YgoCardLibraryMonsterStatSortAxis.Atk;

        float PackWeightKey(CardModel c) =>
            c is YgoDuelistCard y ? y.PackWeightMultiplier : 1f;

        int StatKey(CardModel c)
        {
            if (c is BaseMonsterCard bm)
                return useAtk ? bm.BaseAtk : bm.BaseDef;
            return descending ? int.MinValue : int.MaxValue;
        }

        int LockedBucket(CardModel c)
        {
            var vis = (ModelVisibility)GetCardVisibilityMethod.Invoke(__instance, [c])!;
            return vis == ModelVisibility.Locked ? 1 : 0;
        }

        List<(CardModel c, int i)> indexed = cards.Select((c, i) => (c, i)).ToList();
        IEnumerable<CardModel> ordered = state.PrimaryMonsterStatSort == YgoCardLibraryMonsterStatSortAxis.PackWeight
            ? (descending
                ? indexed.OrderBy(x => LockedBucket(x.c)).ThenByDescending(x => PackWeightKey(x.c)).ThenBy(x => x.i).Select(x => x.c)
                : indexed.OrderBy(x => LockedBucket(x.c)).ThenBy(x => PackWeightKey(x.c)).ThenBy(x => x.i).Select(x => x.c))
            : (descending
                ? indexed.OrderBy(x => LockedBucket(x.c)).ThenByDescending(x => StatKey(x.c)).ThenBy(x => x.i).Select(x => x.c)
                : indexed.OrderBy(x => LockedBucket(x.c)).ThenBy(x => StatKey(x.c)).ThenBy(x => x.i).Select(x => x.c));

        cards.Clear();
        cards.AddRange(ordered);
    }

    static NCardLibrary? FindParentCardLibrary(Node start)
    {
        for (Node? n = start; n != null; n = n.GetParent())
        {
            if (n is NCardLibrary lib)
                return lib;
        }

        return null;
    }
}
