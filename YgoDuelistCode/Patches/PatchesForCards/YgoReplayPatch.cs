using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Universal YGO replay: suppress vanilla multi-play on zone-bound <see cref="CardModel"/> instances and drain
/// sequential persistent clones after the first paid resolve.
/// </summary>
public static class YgoReplayPatch
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.CreateClone))]
    public static class YgoCardCreateCloneFromZonePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CardModel __instance, ref CardModel __result) =>
            !YgoReplayCloneFactory.TryCloneFromYgoZonePile(__instance, ref __result);
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardPlayCount))]
    public static class ModifyCardPlayCountPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CardModel card, ref int __result)
        {
            YgoReplayCoordinator.TryCapturePlayCount(card, ref __result);
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    public static class OnPlayWrapperPatch
    {
        [HarmonyPrefix]
        public static void Prefix(
            CardModel __instance,
            Creature? target,
            bool isAutoPlay)
        {
            if (isAutoPlay || YgoReplayCoordinator.IsReplaySpawn(__instance))
                return;

            YgoReplayCoordinator.BeginFirstPlay(__instance, target);
        }

        [HarmonyPostfix]
        public static async Task Postfix(
            Task __originalMethod,
            CardModel __instance,
            PlayerChoiceContext choiceContext,
            Creature? target,
            bool isAutoPlay)
        {
            await __originalMethod;

            if (isAutoPlay || YgoReplayCoordinator.IsReplaySpawn(__instance) || YgoReplayCoordinator.ProcessingReplay)
                return;

            await YgoReplayCoordinator.CompleteFirstPlayAndDrainAsync(choiceContext, __instance, target);
        }
    }

    [HarmonyPatch(typeof(DuelMonsterFieldRegistry), nameof(DuelMonsterFieldRegistry.RegisterSummon))]
    public static class RegisterSummonPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BaseMonsterCard card)
        {
            YgoReplayCoordinator.NoteSummonedOutput(card);
        }
    }

    [HarmonyPatch(typeof(NormalMonsterCard), nameof(NormalMonsterCard.CombatAction))]
    public static class HandSummonCombatActionCapturePatch
    {
        [HarmonyPrefix]
        public static void Prefix(NormalMonsterCard __instance, CardPlay cardPlay)
        {
            YgoReplayCoordinator.NoteHandSummonCombatAction(__instance, cardPlay);
        }
    }

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndCombatInternal))]
    public static class CombatEndClearPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            YgoReplayCoordinator.ClearCombatState();
        }
    }
}
