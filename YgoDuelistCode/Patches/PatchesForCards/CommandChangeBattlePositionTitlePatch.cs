using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.Title"/> is not virtual; override display title for the dynamic command option.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class CommandChangeBattlePositionTitlePatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not MonsterCommandCard mcmd || !mcmd.TryPatchLocalizedTitleForCardModelTitleGetter(__instance, ref __result))
            return;
    }
}
