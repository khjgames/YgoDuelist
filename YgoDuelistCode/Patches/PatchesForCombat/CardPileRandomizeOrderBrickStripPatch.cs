using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCombat;

/// <summary>
/// After the initial combat draw pile shuffle, eject <see cref="Cards.Core.IYgoBrickCard"/> cards to the discard pile.
/// </summary>
[HarmonyPatch(typeof(CardPile), nameof(CardPile.RandomizeOrderInternal))]
public static class CardPileRandomizeOrderBrickStripPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardPile __instance, Player player, Rng rng, CombatState state)
    {
        _ = rng;
        _ = state;
        if (__instance.Type != PileType.Draw || player.PlayerCombatState == null)
            return;
        YgoBrickCardBootstrap.StripBricksFromPlayerCombatPiles(player);
    }
}
