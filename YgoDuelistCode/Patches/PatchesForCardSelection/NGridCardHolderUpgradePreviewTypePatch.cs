using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCardSelection;

/// <summary>
/// Vanilla <see cref="NGridCardHolder.SetIsPreviewingUpgrade"/> swaps in the upgraded clone and calls <see cref="NCard.ShowUpgradePreview"/>,
/// but never sets <see cref="CardModel.UpgradePreviewType"/>. YGO cards use that for energy, stats, and alternate upgrade lines (same as
/// <see cref="MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckUpgradeSelectScreen"/> which sets <see cref="CardUpgradePreviewType.Deck"/>).
/// </summary>
[HarmonyPatch(typeof(NGridCardHolder), nameof(NGridCardHolder.SetIsPreviewingUpgrade))]
internal static class NGridCardHolderUpgradePreviewTypePatch
{
    private static void Prefix(NGridCardHolder __instance, bool showUpgradePreview, ref AbstractMonsterCard? __state)
    {
        if (!showUpgradePreview)
            return;
        __state = __instance.CardNode?.Model as AbstractMonsterCard;
    }

    private static void Postfix(NGridCardHolder __instance, bool showUpgradePreview, AbstractMonsterCard? __state)
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

        if (__state != null && node.Model is AbstractMonsterCard previewMonster)
            previewMonster.CopyDisplayFormFrom(__state);

        node.Model.UpgradePreviewType = CardUpgradePreviewType.Deck;
        node.ShowUpgradePreview();
    }
}
