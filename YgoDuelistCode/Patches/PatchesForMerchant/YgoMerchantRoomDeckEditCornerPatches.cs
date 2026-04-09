using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Bottom-left deck edit row on the merchant scene (hidden while the buy grid / shop UI is open).
/// </summary>
[HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._Ready))]
[HarmonyAfter("YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant.YgoMerchantRoomCardTraderReadyPatch")]
public static class YgoMerchantRoomDeckEditCornerReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantRoom __instance)
    {
        MerchantInventory? inv = __instance.Inventory?.Inventory;
        Player? player = inv?.Player;
        if (player == null)
            return;

        YgoDeckEditCornerUi.EnsureShop(__instance, player);
    }
}

[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
public static class NMerchantInventoryOpenHideDeckEditCornerPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        YgoDeckEditCornerUi.SetShopLayerVisible(false);
    }
}

[HarmonyPatch(typeof(NMerchantInventory), "Close")]
public static class NMerchantInventoryCloseShowDeckEditCornerPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        YgoDeckEditCornerUi.SetShopLayerVisible(true);
    }
}
