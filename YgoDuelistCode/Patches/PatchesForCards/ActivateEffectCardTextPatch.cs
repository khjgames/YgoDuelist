using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.GetDescriptionForPile"/> reads <see cref="CardModel.Description"/> via non-virtual dispatch, so
/// per-source text for commands implementing <see cref="IActivateEffectPileUi"/> must be injected here.
/// Dynamic vars ({Mgc}, {Mgc2}, etc.) live on <see cref="MonsterCommandCard.SourceMonster"/>, not on the command card.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), typeof(PileType), typeof(Creature))]
public static class ActivateEffectCardTextPatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is IActivateEffectPileUi ui && ui.TryGetActivateEffectPileDescription(ref __result))
            return;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class ActivateEffectTitlePatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is IActivateEffectPileUi ui && ui.TryGetActivateEffectPileTitle(ref __result))
            return;
    }
}
