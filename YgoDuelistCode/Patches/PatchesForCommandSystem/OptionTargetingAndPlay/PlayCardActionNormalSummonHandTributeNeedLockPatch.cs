using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Runs after <see cref="PlayCardActionTributeSelectionPatch"/> (priority 850): hand duel monsters that need <b>zero</b>
/// tributes at selection time (e.g. Cost Down → level 3 Labyrinth Wall) still evaluate as level 5 in
/// <see cref="NormalMonsterCard.OnPlay"/> because Cost Down only applies while <see cref="PileType.Hand"/>.
/// Stash the pre-play tribute count under the same net card key as <see cref="TributeSummonPlayPayload"/>.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(799)]
public static class PlayCardActionNormalSummonHandTributeNeedLockPatch
{
    static void Prefix(PlayCardAction __instance)
    {
        if (!CombatManager.Instance.IsInProgress)
            return;

        CardModel? card = __instance.NetCombatCard.ToCardModelOrNull();
        if (card is not NormalMonsterCard nmc || !nmc.CanSummonDuelMonster || card.Pile?.Type != PileType.Hand)
            return;

        // Tribute > 0 uses PlayCardActionTributeSelectionPatch + TributeSummonPlayPayload (runs first at 850).
        if (nmc.TributeReleaseCount > 0)
            return;

        int lockedNeed = nmc.TributeReleaseCount;
        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(__instance);
        NormalSummonHandTributeNeedLock.Set(
            __instance.Player.NetId,
            __instance.NetCombatCard.CombatCardIndex,
            lockedNeed);
    }
}
