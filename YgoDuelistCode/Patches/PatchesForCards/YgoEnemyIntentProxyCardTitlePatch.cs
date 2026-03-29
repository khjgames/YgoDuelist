using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
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

        if (__instance is YgoFortunetellingGuessProxyCard g)
        {
            string key = g.GuessKind switch
            {
                YgoFortuneGuessKind.Spell => "OMINOUS_FORTUNETELLING_GUESS_SPELL",
                YgoFortuneGuessKind.Trap => "OMINOUS_FORTUNETELLING_GUESS_TRAP",
                YgoFortuneGuessKind.Monster => "OMINOUS_FORTUNETELLING_GUESS_MONSTER",
                _ => "OMINOUS_FORTUNETELLING_GUESS_MONSTER"
            };
            __result = new LocString("combat_messages", key).GetFormattedText();
            return false;
        }

        return true;
    }
}
