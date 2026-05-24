using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Services;
using static YgoDuelist.YgoDuelistCode.Services.YgoSerializableDeckLists;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NDeckHistory), nameof(NDeckHistory.LoadDeck))]
public static class NDeckHistoryYgoSplitPatch
{
    public static bool Prefix(
        NDeckHistory __instance,
        Player player,
        IEnumerable<SerializableCard> cards)
    {
        if (!IsYgoCharacter(player.Character.Id))
            return true;

        List<SerializableCard> list = cards.ToList();
        if (!TryReadSplit(list, out SplitResult split))
            return true;

        YgoDeckHistoryDisplay.LoadSplit(__instance, player, split);
        return false;
    }
}
