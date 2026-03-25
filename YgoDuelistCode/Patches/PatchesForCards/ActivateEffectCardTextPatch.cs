using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.GetDescriptionForPile"/> reads <see cref="CardModel.Description"/> via non-virtual dispatch, so
/// per-source text for <see cref="Activate_Effect"/> must be injected here. Title uses shared <c>YGODUELIST-ACTIVATE_EFFECT.title</c>.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), typeof(PileType), typeof(Creature))]
public static class ActivateEffectCardTextPatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not Activate_Effect { SourceMonster: IMonsterActivatedEffect impl })
            return;

        var loc = new LocString("cards", impl.ActivatedEffectDescriptionLocKey);
        string text = loc.GetFormattedText();
        if (!string.IsNullOrEmpty(text))
            __result = text;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class ActivateEffectTitlePatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not Activate_Effect)
            return;

        var loc = new LocString("cards", "YGODUELIST-ACTIVATE_EFFECT.title");
        string text = loc.GetFormattedText();
        if (!string.IsNullOrEmpty(text))
            __result = text;
    }
}
