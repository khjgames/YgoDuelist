using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per-player pack reward state (e.g. owed rare upgrades when a rare slot cannot be filled).
/// Runtime cache: <see cref="OwedRareCardVouchers"/> is written to the YGO save marker on save and restored in <c>LoadInventory</c>.
/// </summary>
public sealed class YgoPackRewardProgressState
{
    public int OwedRareCardVouchers;
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
}
