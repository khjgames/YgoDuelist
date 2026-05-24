using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRewards;

/// <summary>
/// YgoDuelist combat rewards: 20% chance to add a single optional <see cref="BaseYgoPowerCard"/> offer alongside packs.
/// </summary>
[HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
public static class YgoCombatPowerCardRewardPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player player, AbstractRoom room, ref List<Reward> __result)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return;

        if (room is not CombatRoom)
            return;

        if (room.RoomType is not (RoomType.Monster or RoomType.Elite or RoomType.Boss))
            return;

        CardReward? bonus = YgoCombatPowerCardRewardOffer.TryCreateIfRolled(player, room.RoomType);
        if (bonus != null)
            __result.Add(bonus);
    }
}
