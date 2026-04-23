using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Swaps to <c>.description_upgraded</c> for <see cref="YgoDuelistCard.UseAlternateUpgradedDescription"/> or <see cref="MonsterCommandCard.UseAlternateUpgradedDescription"/> when the card is upgraded or shown in upgrade preview.
/// Monsters use <see cref="AbstractMonsterCard.GetDescriptionLocString"/> for all four base keys plus <c>_upgraded</c> variants; do not intercept here.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
public static class YgoAlternateUpgradedDescriptionPatch
{
    static bool Prefix(CardModel __instance, ref LocString __result)
    {
        if (__instance is AbstractMonsterCard)
            return true;

        bool useAlternate = __instance is YgoDuelistCard ygo && ygo.UseAlternateUpgradedDescription
            || __instance is MonsterCommandCard mcc && mcc.UseAlternateUpgradedDescription;
        if (!useAlternate)
            return true;
        bool showUpgraded = __instance is YgoDuelistCard ygoCard
            ? ygoCard.IsUpgradedOrPreviewActive
            : __instance.IsUpgraded || __instance.UpgradePreviewType != CardUpgradePreviewType.None;
        if (!showUpgraded)
            return true;
        var loc = new LocString("cards", __instance.Id.Entry + ".description_upgraded");
        if (!loc.Exists())
            return true;
        __result = loc;
        return false;
    }
}
