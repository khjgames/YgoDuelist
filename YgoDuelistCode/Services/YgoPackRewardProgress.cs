using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per-player pack reward state (owed rare vouchers, pack tag fatigue accumulators).
/// Runtime cache; vouchers and tag balance written to the YGO save marker on save.
/// </summary>
public sealed class YgoPackRewardProgressState
{
    public const int MaxTagAccumPerTag = 10_000;

    public int OwedRareCardVouchers;

    /// <summary>Single-bit <see cref="YgoCardPackTags"/> values as <c>long</c> → post bump+Tetris accumulation.</summary>
    public Dictionary<long, int> PackTagAccumByFlag { get; } = new();

    public int GetPackTagAccum(YgoCardPackTags singleBit) =>
        PackTagAccumByFlag.GetValueOrDefault((long)singleBit, 0);

    /// <summary>After a tag is placed on a pack mask: add bump, then Tetris across all participating tags.</summary>
    public void ApplyPackTagPickBumpAndTetris(YgoCardPackTags singleBit, int bump)
    {
        long k = (long)singleBit;
        int next = Math.Clamp(PackTagAccumByFlag.GetValueOrDefault(k, 0) + bump, 0, MaxTagAccumPerTag);
        PackTagAccumByFlag[k] = next;
        ApplyPackTagTetris();
    }

    public void ApplyPackTagTetris()
    {
        int min = int.MaxValue;
        foreach (long bit in YgoPackParticipatingTags.AllFlagValues)
        {
            int v = PackTagAccumByFlag.GetValueOrDefault(bit, 0);
            if (v < min)
                min = v;
        }

        if (min == int.MaxValue)
            return;

        foreach (long bit in YgoPackParticipatingTags.AllFlagValues)
        {
            int v = PackTagAccumByFlag.GetValueOrDefault(bit, 0) - min;
            if (v <= 0)
                PackTagAccumByFlag.Remove(bit);
            else
                PackTagAccumByFlag[bit] = v;
        }
    }

    public bool HasPackTagBalanceToPersist() =>
        YgoPackTagBalanceSerializer.HasAnyNonZeroAccum(PackTagAccumByFlag);

    public void LoadPackTagBalanceFromSave(string? blob) =>
        YgoPackTagBalanceSerializer.ParseInto(blob, PackTagAccumByFlag);
}

public static class YgoPackRewardProgress
{
    private static readonly ConditionalWeakTable<Player, YgoPackRewardProgressState> Table = new();

    public static YgoPackRewardProgressState For(Player player) =>
        Table.GetValue(player, static _ => new YgoPackRewardProgressState());

    public static void SetOwedRareLoadedFromSave(Player player, int owedRareCardVouchers)
    {
        For(player).OwedRareCardVouchers = Math.Clamp(
            owedRareCardVouchers,
            0,
            YgoSaveTrunkSideMarkerCard.MaxSerializedOwedRareVouchers);
    }

    public static void SetPackTagBalanceLoadedFromSave(Player player, string? blob) =>
        For(player).LoadPackTagBalanceFromSave(blob);
}
