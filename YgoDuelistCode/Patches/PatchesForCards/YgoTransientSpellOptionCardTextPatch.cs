using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.GetDescriptionForPile"/> / <see cref="CardModel.Title"/> use the card id by default; transient options use per-instance loc keys.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), typeof(PileType), typeof(Creature))]
public static class YgoTransientSpellOptionDescriptionPatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not YgoTransientSpellOptionCommandCard opt || !opt.IsConfigured)
            return;
        if (string.IsNullOrEmpty(opt.DescriptionCardsLocKey))
            return;

        var loc = new LocString("cards", opt.DescriptionCardsLocKey);
        if (opt.DynamicVarSource != null)
            opt.DynamicVarSource.DynamicVars.AddTo(loc);
        string text = loc.GetFormattedText();
        if (!string.IsNullOrEmpty(text))
            __result = text;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class YgoTransientSpellOptionTitlePatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not YgoTransientSpellOptionCommandCard opt || !opt.IsConfigured)
            return;
        if (string.IsNullOrEmpty(opt.TitleCardsLocKey))
            return;

        var loc = new LocString("cards", opt.TitleCardsLocKey);
        string text = loc.GetFormattedText();
        if (!string.IsNullOrEmpty(text))
            __result = text;
    }
}
