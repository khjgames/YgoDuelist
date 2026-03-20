using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class CommandToggleTributeSacrificeTitlePatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not Command_Toggle_Tribute_Sacrifice cmd || cmd.SourceMonster is not BaseMonsterCard monster)
            return;

        var player = cmd.Owner;
        bool marked = player != null && TributeMaterialMarkTracker.IsMarked(player, monster);
        var key = marked
            ? "COMMAND_TOGGLE_TRIBUTE_SACRIFICE.unmark_title"
            : "COMMAND_TOGGLE_TRIBUTE_SACRIFICE.mark_title";

        __result = new LocString("cards", key).GetFormattedText();
        if (__instance.IsUpgraded)
        {
            if (__instance.MaxUpgradeLevel > 1)
                __result = $"{__result}+{__instance.CurrentUpgradeLevel}";
            else
                __result += "+";
        }
    }
}
