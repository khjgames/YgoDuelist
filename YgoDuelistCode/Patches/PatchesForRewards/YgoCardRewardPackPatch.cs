using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRewards;

/// <summary>
/// YgoDuelist: combat card rewards (non-boss) use three YGO tag packs and <see cref="MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromChooseABundleScreen"/>.
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
