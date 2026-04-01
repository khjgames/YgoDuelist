using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NRelicInventoryHolder), "RefreshAmount")]
public static class YgoRelicInventoryCounterTextPatch
{
    [HarmonyPostfix]
    private static void Postfix(NRelicInventoryHolder __instance)
    {
        NRelic relicNode = __instance.GetNode<NRelic>("%Relic");
        if (relicNode.Model is not TrunkSideDeckRelic trunkSideRelic)
            return;
        if (!trunkSideRelic.ShowCounter)
            return;

        MegaLabel amountLabel = __instance.GetNode<MegaLabel>("%AmountLabel");
        Player? owner = trunkSideRelic.Owner;
        int trunkCount = owner == null ? 0 : PlayerRunTrunk.GetOrCreatePile(owner).Cards.Count;
        int sideCount = owner == null ? 0 : PlayerRunSideDeck.GetOrCreatePile(owner).Cards.Count;
        amountLabel.SetTextAutoSize($"{trunkCount}/{sideCount}");
    }
}

[HarmonyPatch(typeof(NTopBarDeckButton), "OnPileContentsChanged")]
public static class YgoTopBarDeckCountTextPatch
{
    [HarmonyPostfix]
    private static void Postfix(NTopBarDeckButton __instance)
    {
        Player? player = Traverse.Create(__instance).Field<Player>("_player").Value;
        if (player == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        int currentDeckCount = player.Deck.Cards.Count;
        int minDeckCount = YgoPlayerMinimumDeck.Get(player);
        MegaLabel countLabel = __instance.GetNode<MegaLabel>("DeckCardCount");
        countLabel.SetTextAutoSize($"{currentDeckCount}/{minDeckCount}");
    }
}
