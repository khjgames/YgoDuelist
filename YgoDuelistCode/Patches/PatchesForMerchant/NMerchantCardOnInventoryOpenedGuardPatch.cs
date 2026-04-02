using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Vanilla iterates every <see cref="NMerchantCard"/> under the character/colorless containers; unfilled template slots
/// leave <c>_cardEntry</c> null and <see cref="NMerchantCard.OnInventoryOpened"/> throws. Skip those safely.
/// </summary>
[HarmonyPatch(typeof(NMerchantCard), nameof(NMerchantCard.OnInventoryOpened))]
public static class NMerchantCardOnInventoryOpenedGuardPatch
{
    private static readonly AccessTools.FieldRef<NMerchantCard, MerchantCardEntry> CardEntryField =
        AccessTools.FieldRefAccess<NMerchantCard, MerchantCardEntry>("_cardEntry");

    [HarmonyPrefix]
    public static bool Prefix(NMerchantCard __instance) => CardEntryField(__instance) != null;
}
