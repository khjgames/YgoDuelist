using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.Title"/> is not virtual; override display title for the dynamic command option.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class CommandChangeBattlePositionTitlePatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not Command_Change_Battle_Position cmd || cmd.SourceMonster is not AbstractMonsterCard monster)
            return;

        var key = monster.Type == CardType.Skill
            ? "COMMAND_CHANGE_BATTLE_POSITION.change_to_attack_title"
            : "COMMAND_CHANGE_BATTLE_POSITION.change_to_defense_title";

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
