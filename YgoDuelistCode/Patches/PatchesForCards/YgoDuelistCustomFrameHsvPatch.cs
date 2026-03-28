using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Per-card frame tint (pool HSV applies to all pool cards; basic Strike/Defend use neutral gray).
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.FrameMaterial), MethodType.Getter)]
public static class YgoDuelistCustomFrameHsvPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref Material __result)
    {
        if (__instance is not YgoDuelistCard ygo || ygo.CustomFrameTintHsv is not { } hsv)
            return;

        __result = ShaderUtils.GenerateHsv(hsv.H, hsv.S, hsv.V);
    }
}
