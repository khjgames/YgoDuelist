using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// YGO deck cards (<see cref="IYgoCard"/>) only transform into other cards with the same
/// <see cref="IYgoCard.YgoCardType"/> (fusion→fusion, spell→spell, etc.), analogous to curse/status
/// staying in their rarity band in the base <see cref="CardFactory"/> transform pipeline.
/// </summary>
[HarmonyPatch]
public static class CardFactoryYgoTransformSameTypePatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() =>
        typeof(CardFactory).GetMethod(
            "GetFilteredTransformationOptions",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(CardModel), typeof(IEnumerable<CardModel>), typeof(bool) },
            modifiers: null)
        ?? throw new InvalidOperationException("CardFactory.GetFilteredTransformationOptions not found");

    [HarmonyPostfix]
    public static void Postfix(CardModel original, IEnumerable<CardModel> originalOptions, ref CardModel[] __result)
    {
        if (original is not IYgoCard origYgo || original is MonsterCommandCard)
            return;

        YgoCardType t = origYgo.YgoCardType;
        CardModel[] narrowed = __result.Where(c => c is IYgoCard y && y.YgoCardType == t).ToArray();
        if (narrowed.Length == 0)
        {
            throw new InvalidOperationException(
                "All transformation options are invalid after YgoDuelist same-type filter. Original: "
                + original.Id
                + ", YgoCardType: "
                + t
                + ", prior options: "
                + string.Join(",", __result.AsEnumerable()));
        }

        __result = narrowed;
    }
}
