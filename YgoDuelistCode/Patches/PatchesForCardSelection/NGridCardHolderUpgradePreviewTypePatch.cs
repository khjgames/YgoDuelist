using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCardSelection;

/// <summary>
/// Vanilla <see cref="NGridCardHolder.SetIsPreviewingUpgrade"/> swaps in the upgraded clone and calls <see cref="NCard.ShowUpgradePreview"/>,
/// but never sets <see cref="CardModel.UpgradePreviewType"/>. YGO cards use that for energy, stats, and alternate upgrade lines (same as
/// <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckUpgradeSelectScreen"/> which sets <see cref="CardUpgradePreviewType.Deck"/>).
/// </summary>
[HarmonyPatch(typeof(NGridCardHolder), nameof(NGridCardHolder.SetIsPreviewingUpgrade))]
internal static class NGridCardHolderUpgradePreviewTypePatch
{
    private static void Postfix(NGridCardHolder __instance, bool showUpgradePreview)
    {
        if (!__instance.Visible)
            return;

        NCard? node = __instance.CardNode;
        CardModel? baseCard = __instance.CardModel;
        if (node?.Model == null || baseCard == null)
            return;

        if (!showUpgradePreview)
            return;

        if (!baseCard.IsUpgradable)
            return;

        node.Model.UpgradePreviewType = CardUpgradePreviewType.Deck;
        node.ShowUpgradePreview();
    }
}
