using System;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Vanilla <see cref="NetCombatCardDb"/> only hooks <see cref="MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState.AllPiles"/>
/// (hand/draw/discard/exhaust/play). Custom YGO piles never raise that subscription path, so cards that appear only in
/// those zones were missing from the combat id map — MP <see cref="PlayCardAction"/> then fails with
/// "Could not map ID N to any card!" on peers that assign fewer ids. This mirrors vanilla's <c>IdCardIfNecessary</c> for
/// every mutable card added to any combat pile.
/// </summary>
[HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
public static class CardPileAddInternalNetCombatCardIdPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardPile __instance, CardModel card)
    {
        if (!__instance.IsCombatPile || CombatManager.Instance?.IsInProgress != true)
            return;
        if (card == null || !card.IsMutable)
            return;
        try
        {
            NetCombatCardDb.Instance.IdCardForTesting(card);
            YgoMpNetCardAssignLog.AfterIdAssigned(__instance, card);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoDuelist][MP] CardPileAddInternalNetCombatCardIdPatch: IdCardForTesting failed: {ex.Message}");
        }
    }
}
