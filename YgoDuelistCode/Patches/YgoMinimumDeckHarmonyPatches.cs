using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

internal static class YgoMinimumDeckTaskHooks
{
    public static async Task<bool> DecrementMinAfterTrue(Task<bool> inner, Player player, int removals)
    {
        bool ok = await inner;
        if (ok)
            YgoPlayerMinimumDeck.DecreaseAfterVoluntaryRemovals(player, removals);
        return ok;
    }
}

[HarmonyPatch(typeof(CardSelectCmd))]
public static class CardSelectCmdFromDeckForRemovalYgoMinimumPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(CardSelectCmd),
            nameof(CardSelectCmd.FromDeckForRemoval),
            new[] { typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>) });

    public static void Prefix(Player player, ref Func<CardModel, bool>? filter)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        int min = YgoPlayerMinimumDeck.Get(player);
        Func<CardModel, bool>? inner = filter;
        filter = c =>
        {
            if (player.Deck.Cards.Count <= min)
                return false;
            return inner == null || inner(c);
        };
    }
}

[HarmonyPatch(typeof(RewardSynchronizer), nameof(RewardSynchronizer.DoLocalCardRemoval))]
public static class YgoRewardSynchronizerCardRemovalMinimumDeckPatch
{
    public static void Postfix(RewardSynchronizer __instance, ref Task<bool> __result)
    {
        Player player = Traverse.Create(__instance).Property<Player>("LocalPlayer").Value;
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        __result = YgoMinimumDeckTaskHooks.DecrementMinAfterTrue(__result, player, 1);
    }
}

[HarmonyPatch(typeof(CookRestSiteOption), nameof(CookRestSiteOption.OnSelect))]
public static class YgoCookRestSiteMinimumDeckPatch
{
    public static void Postfix(CookRestSiteOption __instance, ref Task<bool> __result)
    {
        Player owner = Traverse.Create(__instance).Property<Player>("Owner").Value;
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(owner))
            return;

        __result = YgoMinimumDeckTaskHooks.DecrementMinAfterTrue(__result, owner, 2);
    }
}
