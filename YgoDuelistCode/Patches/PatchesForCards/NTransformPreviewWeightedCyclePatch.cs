using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// Transform preview cycles replacement candidates with probability proportional to
/// <see cref="YgoDuelistCard.AdjustedPackWeightMultiplier"/> instead of a flat shuffle.
/// </summary>
[HarmonyPatch(typeof(NTransformPreview), "CycleThroughCards")]
public static class NTransformPreviewWeightedCyclePatch
{
    private static readonly FieldInfo CancelTokenField =
        AccessTools.Field(typeof(NTransformPreview), "_cancelTokenSource")
        ?? throw new InvalidOperationException("NTransformPreview._cancelTokenSource not found");

    [HarmonyPrefix]
    public static bool Prefix(
        NTransformPreview __instance,
        NPreviewCardHolder holder,
        CardPile cardPile,
        IEnumerable<CardModel> possibleTransformations)
    {
        CancelTokenField.SetValue(__instance, new CancellationTokenSource());
        TaskHelper.RunSafely(RunWeightedPreviewAsync(__instance, holder, cardPile, possibleTransformations));
        return false;
    }

    private static async Task RunWeightedPreviewAsync(
        NTransformPreview preview,
        NPreviewCardHolder holder,
        CardPile cardPile,
        IEnumerable<CardModel> possibleTransformations)
    {
        CancellationTokenSource? cts = CancelTokenField.GetValue(preview) as CancellationTokenSource;
        if (cts == null)
            return;

        List<CardModel> cards = possibleTransformations.ToList();
        while (!cts.IsCancellationRequested)
        {
            CardModel next = Rng.Chaotic.WeightedNextItem(cards, YgoTransformSelectionWeight.ForCard)
                ?? cards[0];
            holder.ReassignToCard(next, cardPile.Type, null, ModelVisibility.Visible);
            if (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
            {
                await Task.Delay(200);
            }
            else
            {
                await Cmd.Wait(0.2f, ignoreCombatEnd: true);
            }
        }
    }
}
