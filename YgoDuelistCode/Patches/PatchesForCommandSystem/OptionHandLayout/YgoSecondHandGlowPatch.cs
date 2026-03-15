using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Prevents null-ref in NHandCardHolder.ShouldGlowGold for NYgoOptionCardHolder
/// by providing a safe implementation that doesn't depend on the private _hand
/// field. We approximate the vanilla logic but ignore SelectModeGoldGlowOverride.
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "get_ShouldGlowGold")]
public static class YgoSecondHandGlowPatch
{
    static bool Prefix(NHandCardHolder __instance, ref bool __result)
    {
        if (__instance is not NYgoOptionCardHolder)
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
