using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary><see cref="Fairy_Box_Upkeep_Take_Damage"/> uses <c>.title_upgraded</c> when marked upgraded (Fairy Box+ upkeep UI).</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class YgoFairyBoxUpkeepTitleUpgradedPatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not Fairy_Box_Upkeep_Take_Damage)
            return;
        if (!__instance.IsUpgraded && __instance.UpgradePreviewType == CardUpgradePreviewType.None)
            return;
        var loc = new LocString("cards", __instance.Id.Entry + ".title_upgraded");
        if (loc.Exists())
            __result = loc.GetFormattedText();
    }
}
