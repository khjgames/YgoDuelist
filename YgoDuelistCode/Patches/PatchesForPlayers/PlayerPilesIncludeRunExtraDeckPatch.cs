using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Exposes the run Extra Deck pile on <see cref="Player.Piles"/> so <see cref="MegaCrit.Sts2.Core.Models.CardModel.Pile"/> resolves for fusion cards.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.Piles), MethodType.Getter)]
public static class PlayerPilesIncludeRunExtraDeckPatch
{
    public static void Postfix(Player __instance, ref IEnumerable<CardPile> __result)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(__instance))
            return;

        CardPile? extra = YgoPlayerRunPiles.RunExtraDeck(__instance);
        if (extra == null)
            return;
        __result = __result.Concat(new[] { extra });
    }
}
