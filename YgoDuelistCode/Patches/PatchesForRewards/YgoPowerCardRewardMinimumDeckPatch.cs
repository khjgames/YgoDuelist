using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rewards;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRewards;

/// <summary>
/// Taking the optional combat power-card reward counts as one received card toward YGO minimum deck size.
/// </summary>
[HarmonyPatch(typeof(CardReward), "OnSelect")]
public static class YgoPowerCardRewardMinimumDeckPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardReward __instance, ref Task<bool> __result)
    {
        if (!YgoCombatPowerCardRewardOffer.IsPowerCardBonusReward(__instance))
            return;
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(__instance.Player))
            return;

        __result = ApplyMinimumDeckProgressAfterSelect(__result, __instance);
    }

    private static async Task<bool> ApplyMinimumDeckProgressAfterSelect(Task<bool> inner, CardReward reward)
    {
        Player player = reward.Player;
        int deckBefore = player.Deck.Cards.Count;
        bool consumed = await inner;
        if (player.Deck.Cards.Count <= deckBefore)
            return consumed;

        YgoPlayerMinimumDeck.ApplyPowerCardTakenMinimumDeckIncrease(player);
        YgoTopBarDeckCountTextPatch.RefreshDeckCountLabelForPlayer(player);
        Log.Info(
            $"[YgoDuelist][PowerCardReward] minDeck+1 | minDeckNow={YgoPlayerMinimumDeck.Get(player)} | receivedProgress={YgoPlayerMinimumDeck.GetReceivedCardProgress(player)}");
        return consumed;
    }
}
