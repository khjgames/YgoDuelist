using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Clone fusion monsters from the run Extra Deck into the combat Extra Deck pile (not the draw pile).
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.PopulateCombatState))]
public static class PlayerPopulateCombatStateExtraDeckPatch
{
    [HarmonyPostfix]
    public static void Postfix(Player __instance, Rng rng, CombatState state)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(__instance))
            return;

        PlayerCombatState? pcs = __instance.PlayerCombatState;
        if (pcs == null)
            return;

        CardPile? combatExtra = ExtraDeckPile.CustomType.GetPile(__instance);
        if (combatExtra == null)
            return;

        CardPile runExtra = PlayerRunExtraDeck.GetOrCreatePile(__instance);
        foreach (CardModel c in runExtra.Cards.ToList())
        {
            if (c is not FusionMonsterCard)
                continue;

            CardModel clone = state.CloneCard(c);
            clone.DeckVersion = c;
            combatExtra.AddInternal(clone, -1, silent: true);
        }
    }
}
