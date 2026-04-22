using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// Random transform outcomes use <see cref="YgoDuelistCard.PackWeightMultiplier"/> like pack generation;
/// vanilla cards in the pool stay at weight 1.
/// </summary>
public static class CardFactoryCreateRandomCardForTransformWeightedPatch
{
    private static readonly MethodInfo GetFilteredTransformationOptions =
        AccessTools.Method(
            typeof(CardFactory),
            "GetFilteredTransformationOptions",
            new[] { typeof(CardModel), typeof(IEnumerable<CardModel>), typeof(bool) })
        ?? throw new InvalidOperationException("CardFactory.GetFilteredTransformationOptions not found");

    [HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateRandomCardForTransform), typeof(CardModel), typeof(bool), typeof(Rng))]
    public static class ThreeArg
    {
        [HarmonyPrefix]
        public static bool Prefix(CardModel original, bool isInCombat, Rng rng, ref CardModel __result)
        {
            IEnumerable<CardModel> opts = CardFactory.GetDefaultTransformationOptions(original, isInCombat);
            CardModel[] arr = opts as CardModel[] ?? opts.ToArray();
            __result = PickAndInstantiate(original, arr, rng);
            return false;
        }
    }

    [HarmonyPatch(
        typeof(CardFactory),
        nameof(CardFactory.CreateRandomCardForTransform),
        new[] { typeof(CardModel), typeof(IEnumerable<CardModel>), typeof(bool), typeof(Rng) })]
    public static class FourArg
    {
        [HarmonyPrefix]
        public static bool Prefix(CardModel original, IEnumerable<CardModel> options, bool isInCombat, Rng rng, ref CardModel __result)
        {
            CardModel[] arr = (CardModel[])GetFilteredTransformationOptions.Invoke(null, new object[] { original, options, isInCombat })!;
            __result = PickAndInstantiate(original, arr, rng);
            return false;
        }
    }

    private static CardModel PickAndInstantiate(CardModel original, CardModel[] templates, Rng rng)
    {
        CardModel template = rng.WeightedNextItem(templates, YgoTransformSelectionWeight.ForCard)
            ?? throw new InvalidOperationException("Weighted transform pick returned no card.");
        return original.CardScope.CreateCard(template, original.Owner);
    }
}
