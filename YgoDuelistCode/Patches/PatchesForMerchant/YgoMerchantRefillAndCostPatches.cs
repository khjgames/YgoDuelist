using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>Shop cards stay empty after purchase (singleton pools cannot restock).</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ShouldRefillMerchantEntry))]
public static class YgoMerchantNoRefillPatch
{
    public static void Postfix(MerchantEntry entry, Player player, ref bool __result)
    {
        if (!__result)
            return;
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;
        if (entry is MerchantCardEntry)
            __result = false;
    }
}

/// <summary>YGO buy prices: common / uncommon / rare base with vanilla shop jitter.</summary>
[HarmonyPatch(typeof(MerchantCardEntry), nameof(MerchantCardEntry.CalcCost))]
public static class YgoMerchantCalcCostPatch
{
    [HarmonyPrefix]
    public static bool Prefix(MerchantCardEntry __instance)
    {
        Player? player = Traverse.Create(__instance).Field<Player>("_player").Value;
        if (player == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return true;

        var creation = Traverse.Create(__instance).Property<CardCreationResult?>("CreationResult").Value;
        if (creation == null)
            return true;

        if (creation.Card is not YgoDuelistCard)
            return true;

        int baseCost = creation.Card.Rarity switch
        {
            CardRarity.Rare => 155,
            CardRarity.Uncommon => 88,
            _ => 52
        };

        int cost = Mathf.RoundToInt(baseCost * player.PlayerRng.Shops.NextFloat(0.95f, 1.05f));
        if (Traverse.Create(__instance).Property<bool>("IsOnSale").Value)
            cost /= 2;

        Traverse.Create(__instance).Field<int>("_cost").Value = cost;
        return false;
    }
}
