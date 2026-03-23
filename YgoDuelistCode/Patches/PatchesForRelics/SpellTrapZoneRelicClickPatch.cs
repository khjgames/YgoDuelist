using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
public static class SpellTrapZoneRelicClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix(RelicModel model)
    {
        if (!SpellTrapZoneRelic.IsSpellTrapZoneRelic(model))
            return true;

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
            return true;

        var player = LocalContext.GetMe((IPlayerCollection)runState);
        if (player == null)
            return true;

        if (YgoSecondHandSourceBridge.GetSource(player) == YgoSecondHandSource.SpellTrapZone)
        {
            YgoSecondHandSourceBridge.CloseSpellTrapZoneView(player);
            return false;
        }

        YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.SpellTrapZone);
        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        return false;
    }
}
