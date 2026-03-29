using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRewards;

/// <summary>
/// YgoDuelist: encounter card rewards use three YGO tag packs, then deck / side-deck / trunk grids (<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackRewardFlow"/>).
/// </summary>
[HarmonyPatch(typeof(CardReward), "OnSelect")]
public static class YgoCardRewardPackPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardReward __instance, ref Task<bool> __result)
    {
        if (!YgoCardPackRewardFlow.ShouldReplaceCardRewardSelection(__instance))
            return true;

        __result = YgoCardPackRewardFlow.RunAsync(__instance);
        return false;
    }
}
