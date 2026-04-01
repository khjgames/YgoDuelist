using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// <see cref="PreviewEffect"/> keeps its own card id. Optional <c>.preview_effect.title</c> /
/// <c>.preview_effect.description</c> override the hosted card’s title/body; otherwise the host’s
/// normal title and <see cref="CardModel.GetDescriptionForPile"/> text are used.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class PreviewEffectHostedHoverTitlePatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not PreviewEffect { PreviewHost: { } host })
            return;

        var titleLoc = new LocString("cards", host.Id.Entry + ".preview_effect.title");
        __result = titleLoc.Exists() ? titleLoc.GetFormattedText() : host.Title;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), typeof(PileType), typeof(Creature))]
public static class PreviewEffectHostedHoverDescriptionPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        CardModel __instance,
        ref string __result,
        PileType pileType,
        Creature? target)
    {
        if (__instance is not PreviewEffect { PreviewHost: YgoDuelistCard host })
            return;

        string key = host.Id.Entry + ".preview_effect.description";
        var probe = new LocString("cards", key);
        if (!probe.Exists())
        {
            __result = host.GetDescriptionForPile(pileType, target);
            return;
        }

        var description = new LocString("cards", key);
        host.DynamicVars.AddTo(description);

        UpgradeDisplay upgradeDisplay = host.IsUpgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal;
        description.Add(new IfUpgradedVar(upgradeDisplay));

        bool onTable = pileType == PileType.Hand || pileType == PileType.Play;
        description.Add("OnTable", onTable);
        bool inCombat = CombatManager.Instance != null
            && CombatManager.Instance.IsInProgress
            && (host.Pile?.IsCombatPile ?? pileType.IsCombatPile());
        description.Add("InCombat", inCombat);
        description.Add("IsTargeting", target != null);

        string prefix = EnergyIconHelper.GetPrefix(host);
        description.Add("energyPrefix", prefix);
        description.Add(
            "singleStarIcon",
            "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");

        foreach (var kv in description.Variables)
        {
            if (kv.Value is EnergyVar energyVar)
                energyVar.ColorPrefix = prefix;
        }

        __result = description.GetFormattedText() ?? "";
    }
}
