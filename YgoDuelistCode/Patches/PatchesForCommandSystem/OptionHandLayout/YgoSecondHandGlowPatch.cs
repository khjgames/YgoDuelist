using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Prevents null-ref in NHandCardHolder.ShouldGlowGold for holders that extend
/// <see cref="NHandCardHolder"/> without a real <c>NPlayerHand</c> (option row,
/// Convulsion preview). Approximates vanilla logic without SelectModeGoldGlowOverride.
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "get_ShouldGlowGold")]
public static class YgoSecondHandGlowPatch
{
    static bool Prefix(NHandCardHolder __instance, ref bool __result)
    {
        if (__instance is not NYgoOptionCardHolder && __instance is not NYgoConvulsionPreviewHolder)
            return true; // use vanilla for normal holders

        var card = __instance.CardNode?.Model;
        if (card == null)
        {
            __result = false;
            return false;
        }

        if (!CombatManager.Instance.IsPlayPhase)
        {
            __result = false;
            return false;
        }

        __result = card.CanPlay() && card.ShouldGlowGold;
        return false;
    }
}
