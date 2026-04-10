using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.EnergyCost"/> lazily builds <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardEnergyCost"/> and snapshots <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardEnergyCost.Canonical"/>.
/// <see cref="AbstractMonsterCard.CanonicalEnergyCost"/> depends on upgrade level and preview mode; vanilla <see cref="CardModel.FinalizeUpgradeInternal"/>
/// only runs <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardEnergyCost.FinalizeUpgrade"/> (clears flags) and does not rebuild from canonical, so smith / deck
/// upgrades can leave a stale orb until the cache is cleared.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradePreviewType), MethodType.Setter)]
internal static class CardModelUpgradePreviewEnergyCachePatch
{
    private static void Postfix(CardModel __instance)
    {
        if (__instance is AbstractMonsterCard)
            CardModelEnergyCache.Invalidate(__instance);
    }
}

/// <summary>
/// After a real upgrade (not preview-only), drop the energy snapshot so <see cref="AbstractMonsterCard.CanonicalEnergyCost"/> is re-read.
/// Do not run for spell/trap cards that apply cost via <c>EnergyCost.UpgradeBy</c> — their canonical property stays the base ctor cost.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.FinalizeUpgradeInternal))]
internal static class CardModelFinalizeUpgradeEnergyCachePatch
{
    private static void Postfix(CardModel __instance)
    {
        if (__instance is AbstractMonsterCard)
            CardModelEnergyCache.Invalidate(__instance);
    }
}
