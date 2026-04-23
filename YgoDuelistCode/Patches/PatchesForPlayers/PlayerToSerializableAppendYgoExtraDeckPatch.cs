using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Fusion monsters are stored off the master deck at runtime; append them to the save <c>deck</c> list so loads still restore them.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.ToSerializable))]
public static class PlayerToSerializableAppendYgoExtraDeckPatch
{
    public static void Postfix(Player __instance, ref SerializablePlayer __result)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(__instance))
            return;

        CardPile? extra = YgoPlayerRunPiles.RunExtraDeckIfExists(__instance);
        if (extra == null || extra.Cards.Count == 0)
            return;

        List<SerializableCard> deck = __result.Deck;
        foreach (CardModel c in extra.Cards)
            deck.Add(c.ToSerializable());
    }
}
