using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRestSite;

[HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.BeginRestSite))]
public static class YgoRestSiteSynchronizerBeginChargesPatch
{
    [HarmonyPostfix]
    public static void Postfix(RestSiteSynchronizer __instance)
    {
        IPlayerCollection coll = Traverse.Create(__instance).Field<IPlayerCollection>("_playerCollection").Value;
        foreach (Player p in coll.Players)
            YgoCampfireDeckEditCharges.ResetForRestVisit(p);

        YgoRestSiteMpHealSync.Register(__instance);
    }
}

/// <summary>
/// Bottom-left loot-style “Edit your Deck” row; see <see cref="YgoCampfireDeckEditLayout"/> for offsets / debug flag.
/// </summary>
[HarmonyPatch(typeof(NRestSiteRoom), "UpdateRestSiteOptions")]
public static class NRestSiteRoomYgoCampfireDeckEditCornerPatch
{
    [HarmonyPostfix]
    public static void Postfix(NRestSiteRoom __instance)
    {
        IRunState runState = Traverse.Create(__instance).Field<IRunState>("_runState").Value;
        Player? me = LocalContext.GetMe(runState);
        if (me == null || !YgoPlayerRunPiles.IsYgoRunPlayer(me))
        {
            __instance.GetNodeOrNull<Control>(YgoDeckEditCornerUi.LayerNameRest)?.QueueFree();
            if (YgoCampfireDeckEditLayout.DebugLogCornerUi && me == null)
                GD.Print("[YgoDeckEditCorner] RestSite: LocalContext.GetMe is null — corner not shown");
            return;
        }

        YgoDeckEditCornerUi.EnsureRestSite(__instance, me);
    }
}
