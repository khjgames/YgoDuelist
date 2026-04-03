using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>Lets YGO compendium rarity tickboxes use tri-state click handling without affecting vanilla rarity filters.</summary>
[HarmonyPatch(typeof(NCardRarityTickbox), "OnRelease")]
public static class YgoNCardRarityTickboxTriStatePatch
{
    [HarmonyPrefix]
    static bool Prefix(NCardRarityTickbox __instance)
    {
        if (!YgoTriStateRarityTickRegistry.TryGet(__instance, out YgoTriStateRarityTickController? c) || c == null)
            return true;
        c.OnRelease();
        return false;
    }
}
