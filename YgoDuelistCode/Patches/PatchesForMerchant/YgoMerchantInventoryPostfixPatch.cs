using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Builds YGO shop rows in a <see cref="YgoMerchantInventorySidecar"/> only. Does not modify <see cref="MerchantInventory.CharacterCardEntries"/> or colorless entries.
/// </summary>
[HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
public static class YgoMerchantInventoryPostfixPatch
{
    private static readonly MethodInfo UpdateEntriesMethod =
        AccessTools.DeclaredMethod(typeof(MerchantInventory), "UpdateEntries")!;

    [HarmonyPostfix]
    public static void Postfix(MerchantInventory __result)
    {
        Player player = __result.Player;
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        var offer = YgoMerchantOfferGenerator.Generate(player, player.PlayerRng.Shops);
        if (offer.Slots.Count == 0)
            return;

        var onUpdate = (Action<PurchaseStatus, MerchantEntry>)Delegate.CreateDelegate(
            typeof(Action<PurchaseStatus, MerchantEntry>),
            __result,
            UpdateEntriesMethod);

        int saleIdx = player.PlayerRng.Shops.NextInt(offer.Slots.Count + 3);
        var list = new List<MerchantCardEntry>(offer.Slots.Count);

        for (int i = 0; i < offer.Slots.Count; i++)
        {
            YgoMerchantOfferGenerator.ShopSlot slot = offer.Slots[i];
            var entry = new MerchantCardEntry(player, __result, new[] { slot.Template }, slot.Rarity);
            entry.Populate();
            if (saleIdx == i)
                entry.SetOnSale();
            entry.PurchaseCompleted += onUpdate;
            list.Add(entry);
        }

        YgoMerchantInventorySidecarTable.Attach(
            __result,
            new YgoMerchantInventorySidecar
            {
                YgoCardEntries = list,
                PurchaseUpdateHandler = onUpdate
            });
    }
}
