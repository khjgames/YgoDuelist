using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// When the merchant inventory closes while the mod is on YGO buy/sell, internal page state must return to Standard.
/// Otherwise the next <see cref="NMerchantInventory.Open"/> still has vanilla rows hidden and YGO UI visible.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), "Close")]
public static class YgoMerchantInventoryCloseResetPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance)
    {
        YgoMerchantSlotsAddonLayer.TryResetToStandardShopPage(__instance);
        YgoMerchantSlotsAddonLayer.SetAddonNavBarVisible(__instance, false);

        NMerchantRoom? room = NMerchantRoom.Instance;
        if (room == null || room.Inventory != __instance)
            return;

        TextureButton? btn = room.GetNodeOrNull<TextureButton>(YgoMerchantRoomCardTraderReadyPatch.CardTraderButtonName);
        if (btn != null)
            YgoMerchantRoomCardTraderReadyPatch.SyncCardTraderDisabled(room, btn);

        SceneTree? tree = __instance.GetTree();
        if (tree == null)
            return;

        NMerchantRoom roomRef = room;
        SceneTreeTimer t = tree.CreateTimer(0f);
        t.Timeout += () =>
        {
            if (!GodotObject.IsInstanceValid(roomRef))
                return;
            TextureButton? b2 = roomRef.GetNodeOrNull<TextureButton>(YgoMerchantRoomCardTraderReadyPatch.CardTraderButtonName);
            if (b2 != null)
                YgoMerchantRoomCardTraderReadyPatch.SyncCardTraderDisabled(roomRef, b2);
        };
    }
}

/// <summary>Shows the YGO shop tab bar whenever the merchant inventory is open (it is hidden on <see cref="NMerchantInventory.Close"/>).</summary>
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
public static class YgoMerchantInventoryOpenNavPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance)
    {
        YgoMerchantSlotsAddonLayer.SetAddonNavBarVisible(__instance, true);
    }
}
