using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForPotions;

/// <summary>
/// Runs before <see cref="PatchesForMultiplayer.CardFactoryGetDistinctForCombatMpStableOrderPatch"/> so MP short-circuit still uses the same filtered input as single-player.
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat))]
[HarmonyBefore("YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer.CardFactoryGetDistinctForCombatMpStableOrderPatch")]
public static class CardFactoryGetDistinctForCombatYgoPotionCardPoolPatch
{
    [HarmonyPrefix]
    public static void Prefix(Player player, ref IEnumerable<CardModel> cards)
    {
        cards = YgoSkillAttackPotionCardPoolFilter.FilterIfSkillOrAttackPotion(player, cards);
    }
}
