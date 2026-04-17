using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>Upkeep command cards that override <see cref="MonsterCommandCard.ShouldPatchTitleToCardsTitleUpgradedLoc"/> use <c>.title_upgraded</c> (Fairy Box+ upkeep UI).</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class YgoFairyBoxUpkeepTitleUpgradedPatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not MonsterCommandCard mcmd || !mcmd.ShouldPatchTitleToCardsTitleUpgradedLoc(__instance))
            return;
        var loc = new LocString("cards", __instance.Id.Entry + ".title_upgraded");
        if (loc.Exists())
            __result = loc.GetFormattedText();
    }
}
