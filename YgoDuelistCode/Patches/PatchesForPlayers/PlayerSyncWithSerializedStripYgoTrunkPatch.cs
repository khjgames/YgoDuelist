using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="Player.SyncWithSerializedPlayer"/> bypasses <see cref="PlayerLoadInventoryStripYgoTrunkSidePatch"/> (no <c>LoadInventory</c> call).
/// Without stripping the YGO trailer, extra/trunk/side cards and the save marker are loaded into the main <see cref="Player.Deck"/>,
/// which desyncs multiplayer checksums (marker can appear in draw pile). Mirror the load strip here, then restore off-deck piles.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.SyncWithSerializedPlayer))]
public static class PlayerSyncWithSerializedStripYgoTrunkPatch
{
    public static void Prefix(Player __instance, SerializablePlayer player, ref object? __state)
    {
        __state = null;
        if (__instance.Character is not YgoChar)
            return;

        if (PlayerLoadInventoryStripYgoTrunkSidePatch.TryStripTrailerFromDeckList(
                player.Deck,
                __instance.NetId,
                "[YgoDuelist][MP][SyncStrip]",
                out YgoTrunkSideDeckLoadPending? pending))
        {
            __state = pending;
        }
    }

    public static void Postfix(Player __instance, object? __state)
    {
        if (__state is not YgoTrunkSideDeckLoadPending pending)
            return;
        if (__instance.Character is not YgoChar)
            return;

        PlayerLoadInventoryStripYgoTrunkSidePatch.ClearYgoOffDeckPiles(__instance);
        PlayerLoadInventoryStripYgoTrunkSidePatch.RestoreFromPending(__instance, pending, "SyncWithSerializedPlayer");
    }
}
