using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForPotions;

/// <summary>
/// Single-player: replace uniform <see cref="MegaCrit.Sts2.Core.Extensions.IEnumerableExtensions.TakeRandom"/> for YGO skill/attack potions
/// with <see cref="YgoSkillAttackPotionPackWeightedPick"/> (multiplayer uses <see cref="PatchesForMultiplayer.CardFactoryGetDistinctForCombatMpStableOrderPatch"/>).
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat))]
[HarmonyAfter("YgoDuelist.YgoDuelistCode.Patches.PatchesForPotions.CardFactoryGetDistinctForCombatYgoPotionCardPoolPatch")]
[HarmonyBefore("YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer.CardFactoryGetDistinctForCombatMpStableOrderPatch")]
public static class CardFactoryGetDistinctForCombatYgoPotionPackWeightSpPatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        Player player,
        IEnumerable<CardModel> cards,
        int count,
        Rng rng,
        ref IEnumerable<CardModel> __result)
    {
        if (!YgoSkillAttackPotionCardPoolFilter.IsYgoSkillOrAttackPotionContext(player))
            return true;
        if (YgoMpDiagnostics.IsMultiplayer && player?.Creature?.CombatState is CombatState)
            return true;

        IEnumerable<CardModel> afterPlayerCount = FilterForPlayerCount(player.RunState, cards);
        List<CardModel> list = CardFactory.FilterForCombat(afterPlayerCount).ToList();
        list.Sort((a, b) => string.CompareOrdinal(a.Id.Entry, b.Id.Entry));
        List<CardModel> picked = YgoSkillAttackPotionPackWeightedPick.TakeDistinctWeighted(list, count, rng);
        __result = picked.Select(c => player.Creature!.CombatState.CreateCard(c, player));
        return false;
    }

    private static IEnumerable<CardModel> FilterForPlayerCount(IRunState runState, IEnumerable<CardModel> options)
    {
        if (runState.Players.Count > 1)
            return options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly);
        return options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
    }
}
