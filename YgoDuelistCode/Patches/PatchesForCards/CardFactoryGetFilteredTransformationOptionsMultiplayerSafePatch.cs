using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// Multiplayer: transformation candidates that are <see cref="Cards.YgoDuelistCard"/> must carry
/// <see cref="Cards.YgoCardPackTags.MultiplayerSafe"/> (same gate as packs and YGO shop).
/// </summary>
[HarmonyPatch]
public static class CardFactoryGetFilteredTransformationOptionsMultiplayerSafePatch
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
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(CardModel original, ref CardModel[] __result)
    {
        if (original.Owner?.RunState.Players.Count <= 1)
            return;

        var owner = original.Owner!;
        CardModel[] narrowed = __result
            .Where(c => !YgoPackCardCatalog.IsYgoBlockedFromMultiplayerProceduralPools(owner, c))
            .ToArray();
        if (narrowed.Length == 0)
        {
            throw new InvalidOperationException(
                "[YgoDuelist] Transform pool empty after multiplayer-safe filter. Original=" + original.Id);
        }

        __result = narrowed;
    }
}
