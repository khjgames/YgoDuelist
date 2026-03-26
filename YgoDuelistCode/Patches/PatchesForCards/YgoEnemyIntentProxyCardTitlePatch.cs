using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class YgoEnemyIntentProxyCardTitlePatch
{
    public static bool Prefix(CardModel __instance, ref string __result)
    {
        if (__instance is YgoEnemyIntentProxyCard p)
        {
            __result = p.TargetCreature?.Name ?? "?";
            return false;
        }

        if (__instance is YgoDieFaceProxyCard d)
        {
            __result = $"d6: {d.Face}";
            return false;
        }

        return true;
    }
}
