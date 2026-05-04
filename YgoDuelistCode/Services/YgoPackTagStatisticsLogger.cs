using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Prints tag-pool composition once when a YGO run is created/restored, so pack balance can be inspected from godot.log.
/// </summary>
public static class YgoPackTagStatisticsLogger
{
    private const string Prefix = "[YgoDuelist][PackStats]";
    private const string MultiplayerStatsPrefix = "[MULTIPLAYER_STATS]";

    private static readonly object Gate = new();
    private static readonly ConditionalWeakTable<RunState, PrintedMarker> PrintedRuns = new();

    public static void PrintForRun(RunState? runState, string source)
    {
        if (runState == null)
            return;

        lock (Gate)
        {
            if (PrintedRuns.TryGetValue(runState, out _))
                return;
            PrintedRuns.Add(runState, new PrintedMarker());
        }

        foreach (Player player in runState.Players)
        {
            if (player.Character is not YgoChar)
                continue;

            try
            {
                Log.Info(BuildReport(player, source));
            }
            catch (Exception ex)
            {
                Log.Error($"{Prefix} failed source={source} playerNetId={player.NetId}: {ex}");
            }
        }
    }

    private static string BuildReport(Player player, string source)
    {
        YgoCardPackTags[] printedTags = GetPrintedPackTags().ToArray();
        Dictionary<YgoCardPackTags, List<YgoDuelistCard>> pools = BuildPools(player, printedTags, filter: null);
        Dictionary<YgoCardPackTags, List<YgoDuelistCard>> mpPools = BuildPools(player, printedTags, IsEffectiveMultiplayerSafe);

        var sb = new StringBuilder();
        AppendPackStatsSection(
            sb,
            Prefix,
            printedTags,
            pools,
            $"Run pack tag statistics source={source} playerNetId={player.NetId}");

        sb.AppendLine();
        AppendPackStatsSection(
            sb,
            MultiplayerStatsPrefix,
            printedTags,
            mpPools,
            $"Pack tag statistics restricted to cards with MultiplayerSafe in GetEffectivePackTags (same layout as {Prefix}; counts subset for comparison) source={source} playerNetId={player.NetId}");

        return sb.ToString().TrimEnd();
    }

    private static Dictionary<YgoCardPackTags, List<YgoDuelistCard>> BuildPools(
        Player player,
        YgoCardPackTags[] printedTags,
        Func<YgoDuelistCard, bool>? filter)
    {
        return printedTags.ToDictionary(
            tag => tag,
            tag =>
            {
                List<YgoDuelistCard> list = YgoPackCardCatalog.GetUnlockedPool(player, tag).OfType<YgoDuelistCard>().ToList();
                if (filter != null)
                    list = list.Where(filter).ToList();
                return list;
            });
    }

    private static bool IsEffectiveMultiplayerSafe(YgoDuelistCard card) =>
        (YgoPackCardCatalog.GetEffectivePackTags(card) & YgoCardPackTags.MultiplayerSafe) != 0;

    private static void AppendPackStatsSection(
        StringBuilder sb,
        string linePrefix,
        YgoCardPackTags[] printedTags,
        Dictionary<YgoCardPackTags, List<YgoDuelistCard>> pools,
        string headerLine)
    {
        int totalEntries = pools.Values.Sum(pool => pool.Count);
        int uniqueCards = pools.Values.SelectMany(pool => pool).Select(card => card.Id).Distinct().Count();

        AppendLine(sb, linePrefix, headerLine);
        AppendLine(sb, linePrefix, "Excluded pack tags: None, Starter, Normal");
        AppendLine(sb, linePrefix, $"Total number of card entries in all printed pack pools: {totalEntries}");
        AppendLine(sb, linePrefix, $"Unique cards appearing in printed pack pools: {uniqueCards}");

        foreach (YgoCardPackTags tag in printedTags)
        {
            List<YgoDuelistCard> pool = pools[tag];
            AppendLine(sb, linePrefix, "");
            AppendLine(sb, linePrefix, $"{ToPackName(tag)} pack");
            AppendLine(sb, linePrefix, $"total number of cards in the pack pool: {pool.Count}");

            AppendMonsterBucket(sb, linePrefix, "fusion monsters", pool, c => c is AbstractMonsterCard m && m.YgoCardType == YgoCardType.FusionMonster);
            AppendMonsterBucket(sb, linePrefix, "ritual monsters", pool, c => c is AbstractMonsterCard m && m.YgoCardType == YgoCardType.RitualMonster);
            AppendMonsterBucket(sb, linePrefix, "normal monsters", pool, c => IsRegularMonster(c, YgoCardType.Monster));
            AppendMonsterBucket(sb, linePrefix, "effect monsters", pool, c => IsRegularMonster(c, YgoCardType.EffectMonster));
            AppendSimpleBucket(sb, linePrefix, "spells", pool, c => IsPlainSpell(c));
            AppendSimpleBucket(sb, linePrefix, "ritual spells", pool, c => c is RitualSpellCard || GetCardRace(c) == DuelMonsterRace.SpellRitual);
            AppendSimpleBucket(sb, linePrefix, "token spells", pool, c => IsTokenSpell(c));
            AppendMonsterBucket(sb, linePrefix, "trap monsters", pool, IsTrapMonster);
            AppendSimpleBucket(sb, linePrefix, "traps", pool, c => GetCardType(c) == YgoCardType.Trap);
        }
    }

    private static IEnumerable<YgoCardPackTags> GetPrintedPackTags()
    {
        foreach (YgoCardPackTags tag in Enum.GetValues(typeof(YgoCardPackTags)).Cast<YgoCardPackTags>())
        {
            if (tag is YgoCardPackTags.None or YgoCardPackTags.Starter or YgoCardPackTags.Normal)
                continue;

            long raw = (long)tag;
            if (raw > 0 && (raw & (raw - 1)) == 0)
                yield return tag;
        }
    }

    private static void AppendMonsterBucket(
        StringBuilder sb,
        string linePrefix,
        string label,
        List<YgoDuelistCard> pool,
        Func<YgoDuelistCard, bool> predicate)
    {
        List<YgoDuelistCard> cards = pool.Where(predicate).ToList();
        AppendLine(
            sb,
            linePrefix,
            $"{label}: {CountAndPoolPercent(cards.Count, pool.Count)} " +
            $"({LevelBandText(cards, 1, 4, "Levels 1-4")} | " +
            $"{LevelBandText(cards, 5, 6, "Levels 5-6")} | " +
            $"{LevelBandText(cards, 7, int.MaxValue, "Levels 7+")}) " +
            $"Average PackWeightMultiplier for those cards: {AverageWeight(cards)}");
    }

    private static void AppendSimpleBucket(
        StringBuilder sb,
        string linePrefix,
        string label,
        List<YgoDuelistCard> pool,
        Func<YgoDuelistCard, bool> predicate)
    {
        List<YgoDuelistCard> cards = pool.Where(predicate).ToList();
        AppendLine(
            sb,
            linePrefix,
            $"{label}: {CountAndPoolPercent(cards.Count, pool.Count)} " +
            $"Average PackWeightMultiplier for those cards: {AverageWeight(cards)}");
    }

    private static bool IsRegularMonster(YgoDuelistCard card, YgoCardType type) =>
        card is AbstractMonsterCard monster
        && monster.YgoCardType == type
        && !IsTrapMonster(card)
        && card is not IYgoTokenMonster;

    private static bool IsTrapMonster(YgoDuelistCard card) =>
        card.GetType().Namespace?.Contains(".TrapMonster", StringComparison.Ordinal) == true;

    private static bool IsPlainSpell(YgoDuelistCard card) =>
        GetCardType(card) == YgoCardType.Spell
        && card is not RitualSpellCard
        && GetCardRace(card) != DuelMonsterRace.SpellRitual
        && !IsTokenSpell(card);

    private static bool IsTokenSpell(YgoDuelistCard card) =>
        GetCardType(card) == YgoCardType.Spell
        && (card.GetType().Name.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || card.RelatedCards.Any(type => typeof(IYgoTokenMonster).IsAssignableFrom(type)));

    private static YgoCardType? GetCardType(YgoDuelistCard card) =>
        card is IYgoCard ygo ? ygo.YgoCardType : null;

    private static DuelMonsterRace? GetCardRace(YgoDuelistCard card) =>
        card is IYgoCard ygo ? ygo.DuelMonsterRace : null;

    private static string LevelBandText(List<YgoDuelistCard> cards, int minLevel, int maxLevel, string label)
    {
        int count = cards.Count(card => card is AbstractMonsterCard monster
            && monster.DuelMonsterLevel >= minLevel
            && monster.DuelMonsterLevel <= maxLevel);
        return $"{label} {count} / {Percent(count, cards.Count)}";
    }

    private static string CountAndPoolPercent(int count, int poolCount) =>
        $"{count} / {Percent(count, poolCount)}";

    private static string Percent(int count, int total) =>
        total <= 0
            ? "0.0%"
            : (count * 100d / total).ToString("0.0", CultureInfo.InvariantCulture) + "%";

    private static string AverageWeight(List<YgoDuelistCard> cards) =>
        cards.Count == 0
            ? "0.00"
            : cards.Average(card => card.AdjustedPackWeightMultiplier).ToString("0.00", CultureInfo.InvariantCulture);

    private static string ToPackName(YgoCardPackTags tag) =>
        tag.ToString();

    private static void AppendLine(StringBuilder sb, string linePrefix, string line) =>
        sb.Append(linePrefix).Append(' ').AppendLine(line);

    private sealed class PrintedMarker
    {
    }
}
