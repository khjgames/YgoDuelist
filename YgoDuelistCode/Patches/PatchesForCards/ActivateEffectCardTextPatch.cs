using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.GetDescriptionForPile"/> reads <see cref="CardModel.Description"/> via non-virtual dispatch, so
/// per-source text for <see cref="Activate_Effect"/> must be injected here. Title uses shared <c>YGODUELIST-ACTIVATE_EFFECT.title</c>.
/// Dynamic vars ({Mgc}, {Mgc2}, etc.) live on <see cref="MonsterCommandCard.SourceMonster"/>, not on the command card.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), typeof(PileType), typeof(Creature))]
public static class ActivateEffectCardTextPatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is Activate_Effect_2 ae2 && ae2.TryGetPileDescriptionForActivateEffect2(ref __result))
            return;

        if (__instance is Activate_Effect ae && ae.TryGetPileDescriptionForActivateEffect(ref __result))
            return;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
public static class ActivateEffectTitlePatch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is Activate_Effect_2 ae2 && ae2.TryGetTitleForActivateEffect2(ref __result))
            return;

        if (__instance is Activate_Effect ae && ae.TryGetTitleForActivateEffect(ref __result))
            return;
    }
}
