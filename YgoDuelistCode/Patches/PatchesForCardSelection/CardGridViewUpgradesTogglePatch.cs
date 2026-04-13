using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCardSelection;

/// <summary>
/// Adds the same "View Upgrades" toggle as the deck view to Neow starter grid, zone relic grids (GY / Banished / Extra Deck),
/// and <see cref="NDeckCardSelectScreen"/> flows including trunk / side / split editor.
/// </summary>
internal static class CardGridViewUpgradesTogglePatch
{
    [HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen._Ready))]
    private static class SimpleCardSelectScreenViewUpgrades
    {
        [HarmonyPostfix]
        private static void AfterReady(NSimpleCardSelectScreen __instance)
        {
            NCardGrid? grid = __instance.GetNodeOrNull<NCardGrid>("%CardGrid");
            if (grid != null)
                YgoViewUpgradesRowHelper.Attach(__instance, grid);
        }
    }

    [HarmonyPatch(typeof(NDeckCardSelectScreen), nameof(NDeckCardSelectScreen._Ready))]
    private static class DeckCardSelectScreenViewUpgrades
    {
        /// <summary>Runs after <see cref="TrunkSideDeckDeckCardSelectScreenPatch"/> (default priority 0) so %CardGrid and split grids exist.</summary>
        [HarmonyPostfix]
        [HarmonyPriority(-100)]
        private static void AfterReady(NDeckCardSelectScreen __instance)
        {
            NCardGrid? primary = __instance.GetNodeOrNull<NCardGrid>("%CardGrid");
            if (primary == null)
                return;

            NCardGrid? secondary = YgoViewUpgradesRowHelper.FindSplitSideGrid(__instance);
            YgoViewUpgradesRowHelper.Attach(__instance, primary, secondary);
        }
    }
}
