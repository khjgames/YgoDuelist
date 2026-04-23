using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Single hook surface on <see cref="NMerchantInventory.Initialize"/>: Prefix mounts mod-only UI from duplicated
/// <see cref="NMerchantCard"/> templates; Postfix binds YGO <see cref="MerchantCardEntry"/> rows after vanilla fills its slots.
/// Does not replace vanilla fields or patch <c>GetCardSlots</c>. YGO pages hide vanilla shop rows via <see cref="YgoMerchantSlotsAddonLayer"/>; Standard restores them.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
public static class YgoMerchantShopUiPatch
{
    public const string ChromeMetaKey = "YgoMerchantChrome_v1";

    [HarmonyPrefix]
    public static void Prefix(NMerchantInventory __instance, MerchantInventory inventory, MerchantDialogueSet dialogue)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(inventory.Player))
            return;

        if (__instance.HasMeta(ChromeMetaKey))
            return;

        int ygoSlots = YgoMerchantInventorySidecarTable.TryGet(inventory, out YgoMerchantInventorySidecar? sc) && sc != null
            ? sc.YgoCardEntries.Count
            : 0;

        var traverse = Traverse.Create(__instance);
        Control? vanillaChar = traverse.Field<Control>("_characterCardContainer").Value;
        if (vanillaChar == null || vanillaChar.GetChildCount() < 1)
            return;

        if (vanillaChar.GetChild(0) is not NMerchantCard template)
            return;

        YgoMerchantSlotsAddonLayer? layer = YgoMerchantSlotsAddonLayer.TryCreateAndMount(
            __instance,
            inventory.Player,
            ygoSlots,
            template);
        if (layer != null)
            __instance.SetMeta(ChromeMetaKey, true);
    }

    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance, MerchantInventory inventory, MerchantDialogueSet dialogue)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(inventory.Player))
            return;
        if (!__instance.HasMeta(ChromeMetaKey))
            return;

        Control? slots = Traverse.Create(__instance).Field<Control>("_slotsContainer").Value;
        if (slots == null)
            return;

        var layer = slots.GetNodeOrNull<YgoMerchantSlotsAddonLayer>(YgoMerchantSlotsAddonLayer.GodotName);
        layer?.CompleteAfterVanillaInitialize(inventory);
    }
}
