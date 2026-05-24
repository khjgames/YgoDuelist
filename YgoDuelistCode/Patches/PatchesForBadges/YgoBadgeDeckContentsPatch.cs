using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>Deck-content badges should only consider the YgoDuelist main deck, not extra/trunk/side trailer cards.</summary>
[HarmonyPatch]
public static class YgoBadgeDeckContentsPatch
{
    private static readonly FieldInfo LocalPlayerField = AccessTools.Field(typeof(Badge), "_localPlayer")!;

    [HarmonyPatch(typeof(Highlander), nameof(Highlander.IsObtained))]
    [HarmonyPrefix]
    public static bool HighlanderPrefix(Highlander __instance, ref bool __result)
    {
        if (!TryGetMainDeck(__instance, out List<SerializableCard>? main))
            return true;

        List<SerializableCard> nonBasic = main
            .Where(c => SaveUtil.CardOrDeprecated(c.Id).Rarity != CardRarity.Basic)
            .ToList();
        __result = nonBasic.Select(c => c.Id).Distinct().Count() == nonBasic.Count;
        return false;
    }

    [HarmonyPatch(typeof(Curses), nameof(Curses.IsObtained))]
    [HarmonyPrefix]
    public static bool CursesPrefix(Curses __instance, ref bool __result)
    {
        if (!TryGetMainDeck(__instance, out List<SerializableCard>? main))
            return true;

        int curseCount = 0;
        foreach (SerializableCard card in main)
        {
            if (SaveUtil.CardOrDeprecated(card.Id).Type == CardType.Curse)
                curseCount++;
        }

        __result = curseCount >= 5;
        return false;
    }

    [HarmonyPatch(typeof(Honed), nameof(Honed.IsObtained))]
    [HarmonyPrefix]
    public static bool HonedPrefix(Honed __instance, ref bool __result)
    {
        if (!TryGetMainDeck(__instance, out List<SerializableCard>? main))
            return true;

        List<SerializableCard> nonBasic = main
            .Where(c => SaveUtil.CardOrDeprecated(c.Id).Rarity != CardRarity.Basic)
            .ToList();
        __result = nonBasic
            .GroupBy(c => c.Id)
            .Any(g => g.Count() >= 5);
        return false;
    }

    private static bool TryGetMainDeck(Badge badge, out List<SerializableCard> main)
    {
        main = [];
        var player = (SerializablePlayer)LocalPlayerField.GetValue(badge)!;
        if (!YgoSerializableDeckLists.IsYgoCharacter(player.CharacterId))
            return false;

        main = YgoSerializableDeckLists.MainDeckForCharacter(player.CharacterId, player.Deck).ToList();
        return true;
    }
}
