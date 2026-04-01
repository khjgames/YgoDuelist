using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <para>
/// Vanilla <see cref="NHandCardHolder.SetDefaultTargets"/> uses <see cref="NPlayerHand.ActiveHolders"/>.Count
/// as the hand size for <see cref="HandPosHelper"/>, which only supports sizes 1–10. The mod adds
/// <see cref="NYgoOptionCardHolder"/> rows to the same list, so total count can reach 11+ and
/// <c>ReturnHolderToHand</c> throws <see cref="System.ArgumentOutOfRangeException"/>.
/// </para>
/// <para>
/// For main-row holders we use the main-hand-only count and index (same basis as
/// <see cref="YgoSecondHandLayoutPatch"/>). Option holders skip vanilla entirely; layout is from RefreshLayout.
/// </para>
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.SetDefaultTargets))]
public static class YgoSecondHandSetDefaultTargetsPatch
{
    static bool Prefix(NHandCardHolder __instance)
    {
        if (__instance is NYgoOptionCardHolder)
        {
            __instance.UpdateCard();
            return false;
        }

        var handField = AccessTools.Field(typeof(NHandCardHolder), "_hand");
        var hand = handField?.GetValue(__instance) as NPlayerHand;
        if (hand == null)
            return true;

        IReadOnlyList<NHandCardHolder> active = hand.ActiveHolders;
        List<NHandCardHolder> main = active.Where(h => h is not NYgoOptionCardHolder).ToList();
        int mainIdx = main.IndexOf(__instance);
        if (mainIdx < 0)
            return true;

        __instance.ZIndex = 0;
        int mainCount = main.Count;
        __instance.SetTargetPosition(HandPosHelper.GetPosition(mainCount, mainIdx));
        __instance.SetTargetAngle(HandPosHelper.GetAngle(mainCount, mainIdx));
        __instance.SetTargetScale(HandPosHelper.GetScale(mainCount));
        return false;
    }
}
