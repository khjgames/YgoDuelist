using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Matches vanilla <see cref="CardFactory.CreateForReward"/> post-clone steps: <c>RollForUpgrade</c> then
/// <see cref="Hook.TryModifyCardRewardOptions"/> (including <c>TryModifyCardRewardOptionsLate</c>).
/// </summary>
internal static class YgoPackCardRewardHooks
{
    private static readonly MethodInfo RollForUpgradeMethod = AccessTools.DeclaredMethod(
        typeof(CardFactory),
        "RollForUpgrade",
        [typeof(Player), typeof(CardModel), typeof(decimal), typeof(Rng)])!;

    public static void ApplyToPackRow(Player player, IList<CardModel> row, CardCreationOptions options)
    {
        ApplyUpgradeRolls(player, row, options);

        if (options.Flags.HasFlag(CardCreationFlags.NoModifyHooks))
            return;

        List<CardCreationResult> results = row.Select(c => new CardCreationResult(c)).ToList();
        int before = results.Count;

        if (Hook.TryModifyCardRewardOptions(player.RunState, player, results, options, out List<AbstractModel> modifiers))
            TaskHelper.RunSafely(Hook.AfterModifyingCardRewardOptions(player.RunState, modifiers));

        SyncResultsToRow(player, row, results);

        if (results.Count != before || results.Any(r => r.HasBeenModified))
        {
            Log.Info(
                $"[YgoDuelist][PackFlow][Hooks] applied card-reward hooks | rowSize={row.Count} | anyModified={results.Any(r => r.HasBeenModified)}");
        }
    }

    /// <summary>Matches <see cref="MegaCrit.Sts2.Core.Rewards.CardReward.OnRelicObtained"/> for one newly obtained relic.</summary>
    public static void ApplyRelicObtainedToPackRow(Player player, IList<CardModel> row, RelicModel relic, CardCreationOptions options)
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoModifyHooks))
            return;

        List<CardCreationResult> results = row.Select(c => new CardCreationResult(c)).ToList();

        if (relic.TryModifyCardRewardOptions(player, results, options))
            TaskHelper.RunSafely(relic.AfterModifyingRewards());

        if (relic.TryModifyCardRewardOptionsLate(player, results, options))
            TaskHelper.RunSafely(relic.AfterModifyingRewards());

        SyncResultsToRow(player, row, results);
        Log.Info($"[YgoDuelist][PackFlow][Hooks] relic {relic.Id.Entry} modified open pack previews");
    }

    private static void ApplyUpgradeRolls(Player player, IList<CardModel> row, CardCreationOptions options)
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoUpgradeRoll))
            return;

        Rng rng = options.RngOverride ?? player.PlayerRng.Rewards;
        int upgraded = 0;
        foreach (CardModel card in row)
        {
            bool wasUpgraded = card.IsUpgraded;
            RollForUpgradeMethod.Invoke(null, [player, card, 0m, rng]);
            if (!wasUpgraded && card.IsUpgraded)
                upgraded++;
        }

        if (upgraded > 0)
        {
            Log.Info(
                $"[YgoDuelist][PackFlow][Hooks] upgradeRolls | rowSize={row.Count} | upgraded={upgraded} | rng={(options.RngOverride != null ? "override" : "rewards")}");
        }
    }

    private static void SyncResultsToRow(Player player, IList<CardModel> row, List<CardCreationResult> results)
    {
        if (row is not List<CardModel> list)
        {
            ApplySyncToGenericList(player, row, results);
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            CardModel next = results[i].Card;
            if (i < list.Count)
            {
                CardModel prev = list[i];
                if (!ReferenceEquals(prev, next))
                {
                    player.RunState.RemoveCard(prev);
                    list[i] = next;
                }
            }
            else
                list.Add(next);
        }

        while (list.Count > results.Count)
        {
            CardModel tail = list[^1];
            player.RunState.RemoveCard(tail);
            list.RemoveAt(list.Count - 1);
        }
    }

    private static void ApplySyncToGenericList(Player player, IList<CardModel> row, List<CardCreationResult> results)
    {
        var prev = row.ToList();
        row.Clear();
        foreach (CardCreationResult r in results)
        {
            CardModel next = r.Card;
            row.Add(next);
        }

        foreach (CardModel old in prev)
        {
            if (!results.Any(r => ReferenceEquals(r.Card, old)))
                player.RunState.RemoveCard(old);
        }
    }
}
