using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Prevents null refs in NHandCardHolder.SetDefaultTargets for NYgoOptionCardHolder
/// by skipping the base implementation entirely. Our patched
/// NPlayerHand.RefreshLayout is responsible for positioning both main-hand and
/// option-hand holders, so calling SetDefaultTargets on option holders is
/// unnecessary and unsafe (their private _hand field is never set via
/// NHandCardHolder.Create).
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.SetDefaultTargets))]
public static class YgoSecondHandSetDefaultTargetsPatch
{
    static bool Prefix(NHandCardHolder __instance)
    {
        // Let vanilla behaviour run for normal hand holders.
        if (__instance is not NYgoOptionCardHolder)
            return true;

        // For NYgoOptionCardHolder, skip SetDefaultTargets. Layout is handled
        // by our NPlayerHand.RefreshLayout patch instead. But we still want
        // the card visuals (including "can't be played" indicators) to be
        // refreshed when the holder is returned to the hand, so force an
        // UpdateCard here.
        __instance.UpdateCard();
        return false;
    }
}
