using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// Delegates <see cref="CardModel.IsValidTarget"/> refinement to <see cref="YgoDuelistCard.RefineIsValidTarget"/> per card.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
public static class CardModelIsValidTargetYgoRefinePatch
{
    static void Postfix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (__instance is YgoDuelistCard ygo)
            __result = ygo.RefineIsValidTarget(target, __result);
    }
}
