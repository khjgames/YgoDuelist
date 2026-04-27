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
        Dictionary<YgoCardPackTags, List<YgoDuelistCard>> pools = printedTags.ToDictionary(
            tag => tag,
            tag => YgoPackCardCatalog.GetUnlockedPool(player, tag).OfType<YgoDuelistCard>().ToList());

        int totalEntries = pools.Values.Sum(pool => pool.Count);
        int uniqueCards = pools.Values.SelectMany(pool => pool).Select(card => card.Id).Distinct().Count();

        var sb = new StringBuilder();
        AppendLine(sb, $"Run pack tag statistics source={source} playerNetId={player.NetId}");
        AppendLine(sb, "Excluded pack tags: None, Starter, Normal");
        AppendLine(sb, $"Total number of card entries in all printed pack pools: {totalEntries}");
        AppendLine(sb, $"Unique cards appearing in printed pack pools: {uniqueCards}");

        foreach (YgoCardPackTags tag in printedTags)
        {
            List<YgoDuelistCard> pool = pools[tag];
            AppendLine(sb, "");
            AppendLine(sb, $"{ToPackName(tag)} pack");
            AppendLine(sb, $"total number of cards in the pack pool: {pool.Count}");

            AppendMonsterBucket(sb, "fusion monsters", pool, c => c is AbstractMonsterCard m && m.YgoCardType == YgoCardType.FusionMonster);
            AppendMonsterBucket(sb, "ritual monsters", pool, c => c is AbstractMonsterCard m && m.YgoCardType == YgoCardType.RitualMonster);
            AppendMonsterBucket(sb, "normal monsters", pool, c => IsRegularMonster(c, YgoCardType.Monster));
            AppendMonsterBucket(sb, "effect monsters", pool, c => IsRegularMonster(c, YgoCardType.EffectMonster));
            AppendSimpleBucket(sb, "spells", pool, c => IsPlainSpell(c));
            AppendSimpleBucket(sb, "ritual spells", pool, c => c is RitualSpellCard || GetCardRace(c) == DuelMonsterRace.SpellRitual);
            AppendSimpleBucket(sb, "token spells", pool, c => IsTokenSpell(c));
            AppendMonsterBucket(sb, "trap monsters", pool, IsTrapMonster);
            AppendSimpleBucket(sb, "traps", pool, c => GetCardType(c) == YgoCardType.Trap);
        }

        return sb.ToString().TrimEnd();
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
        string label,
        List<YgoDuelistCard> pool,
        Func<YgoDuelistCard, bool> predicate)
    {
        List<YgoDuelistCard> cards = pool.Where(predicate).ToList();
        AppendLine(
            sb,
            $"{label}: {CountAndPoolPercent(cards.Count, pool.Count)} " +
            $"({LevelBandText(cards, 1, 4, "Levels 1-4")} | " +
            $"{LevelBandText(cards, 5, 6, "Levels 5-6")} | " +
            $"{LevelBandText(cards, 7, int.MaxValue, "Levels 7+")}) " +
            $"Average PackWeightMultiplier for those cards: {AverageWeight(cards)}");
    }

    private static void AppendSimpleBucket(
        StringBuilder sb,
        string label,
        List<YgoDuelistCard> pool,
        Func<YgoDuelistCard, bool> predicate)
    {
        List<YgoDuelistCard> cards = pool.Where(predicate).ToList();
        AppendLine(
            sb,
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

    private static void AppendLine(StringBuilder sb, string line) =>
        sb.Append(Prefix).Append(' ').AppendLine(line);

    private sealed class PrintedMarker
    {
    }
}
