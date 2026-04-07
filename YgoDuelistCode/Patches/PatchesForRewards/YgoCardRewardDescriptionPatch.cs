using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRewards;

/// <summary>
/// Map / reward list text for YGO pack rewards: show pack size instead of vanilla "Add a card to your deck."
/// </summary>
[HarmonyPatch(typeof(CardReward), nameof(CardReward.Description), MethodType.Getter)]
public static class YgoCardRewardDescriptionPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardReward __instance, ref LocString __result)
    {
        if (!YgoCardPackRewardFlow.ShouldReplaceCardRewardSelection(__instance))
            return;

        int packSize = YgoCardPackRewardFlow.GetOfferedPackSlotCount(__instance);
        var loc = new LocString("combat_messages", "YGODUELIST-PACK_REWARD_MAP_LABEL");
        loc.AddObj("PackSize", packSize);
        __result = loc;
    }
}
