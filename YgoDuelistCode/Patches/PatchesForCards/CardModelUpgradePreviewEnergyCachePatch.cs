using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.EnergyCost"/> builds <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardEnergyCost"/> once and snapshots canonical cost.
/// <see cref="AbstractMonsterCard.CanonicalEnergyCost"/> depends on <see cref="CardModel.UpgradePreviewType"/> (e.g. high-ATK efficiency tax), so forge / inspect
/// preview must drop the cache when preview mode toggles.
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
