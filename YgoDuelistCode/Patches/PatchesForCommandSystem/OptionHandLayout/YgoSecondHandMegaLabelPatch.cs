using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Skip MegaLabel's font-override assertion for labels created by
/// NYgoOptionCardHolder so they don't throw during _Ready.
/// We don't actually display these indices, so the font is irrelevant.
/// </summary>
[HarmonyPatch(typeof(MegaLabel), nameof(MegaLabel._Ready))]
public static class YgoSecondHandMegaLabelPatch
{
    static bool Prefix(MegaLabel __instance)
    {
        // Only bypass for labels attached to our custom option card holders.
        if (__instance.GetParent() is NYgoOptionCardHolder)
            return false;

        return true;
    }
}
