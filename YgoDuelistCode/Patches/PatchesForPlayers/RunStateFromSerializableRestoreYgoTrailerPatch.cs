using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Applies deferred YGO trailer cards once players are attached to a real <see cref="RunState"/>.
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
public static class RunStateFromSerializableRestoreYgoTrailerPatch
{
    public static void Postfix(RunState __result)
    {
        foreach (Player player in __result.Players)
        {
            if (player.Character is not YgoChar)
                continue;

            if (!PlayerLoadInventoryStripYgoTrunkSidePatch.TryConsumeDeferred(player, out YgoTrunkSideDeckLoadPending pending))
                continue;

            GD.Print(
                $"[YgoDuelist][SaveLoad] RunState.FromSerializable consumed deferred trailer netId={player.NetId} " +
                $"extra={pending.Extra.Count} trunk={pending.Trunk.Count} side={pending.Side.Count}");
            PlayerLoadInventoryStripYgoTrunkSidePatch.Postfix(player, pending);
        }
    }
}
