using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Replaces vanilla global <c>_nextId</c> assignment in <see cref="NetCombatCardDb"/> with
/// <see cref="YgoPerOwnerCombatCardIdAllocator"/> (per-owner bands). Original method is skipped.
/// </summary>
[HarmonyPatch(typeof(NetCombatCardDb), "IdCardIfNecessary", new[] { typeof(CardModel) })]
[HarmonyPriority(Priority.First)]
public static class NetCombatCardDbIdCardIfNecessaryPerOwnerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel card, NetCombatCardDb __instance)
    {
        Traverse tr = Traverse.Create(__instance);
        Dictionary<CardModel, uint> cardToId = tr.Field<Dictionary<CardModel, uint>>("_cardToId").Value;
        Dictionary<uint, CardModel> idToCard = tr.Field<Dictionary<uint, CardModel>>("_idToCard").Value;

        if (cardToId.ContainsKey(card))
            return false;

        uint id = YgoPerOwnerCombatCardIdAllocator.AllocateNextId(card);
        cardToId[card] = id;
        idToCard[id] = card;
        return false;
    }
}

/// <summary>
/// Builds slot map before vanilla assigns any combat card ids during <see cref="NetCombatCardDb.StartCombat"/>.
/// </summary>
[HarmonyPatch(typeof(NetCombatCardDb), nameof(NetCombatCardDb.StartCombat))]
[HarmonyPriority(Priority.First)]
public static class NetCombatCardDbStartCombatPrepareAllocatorPatch
{
    [HarmonyPrefix]
    public static void Prefix(IReadOnlyList<Player> players)
    {
        YgoPerOwnerCombatCardIdAllocator.PrepareCombat(players);
    }
}

/// <summary>
/// When tests clear the combat card db, reset allocator state so the next combat id pass is consistent.
/// </summary>
[HarmonyPatch(typeof(NetCombatCardDb), nameof(NetCombatCardDb.ClearCardsForTesting))]
public static class NetCombatCardDbClearCardsForTestingResetAllocatorPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        YgoPerOwnerCombatCardIdAllocator.PrepareCombat(null);
    }
}
