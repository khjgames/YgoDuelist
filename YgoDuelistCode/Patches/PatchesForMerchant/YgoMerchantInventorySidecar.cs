using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Merchant;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// YGO-only merchant card rows live here so <see cref="MerchantInventory.CharacterCardEntries"/> stays vanilla (5 colored slots).
/// </summary>
public sealed class YgoMerchantInventorySidecar
{
    public required List<MerchantCardEntry> YgoCardEntries { get; init; }
    public required Action<PurchaseStatus, MerchantEntry> PurchaseUpdateHandler { get; init; }
}

public static class YgoMerchantInventorySidecarTable
{
    private static readonly ConditionalWeakTable<MerchantInventory, YgoMerchantInventorySidecar> Table = new();

    public static void Attach(MerchantInventory inventory, YgoMerchantInventorySidecar sidecar) =>
        Table.Add(inventory, sidecar);

    public static bool TryGet(MerchantInventory inventory, out YgoMerchantInventorySidecar? sidecar) =>
        Table.TryGetValue(inventory, out sidecar);
}
