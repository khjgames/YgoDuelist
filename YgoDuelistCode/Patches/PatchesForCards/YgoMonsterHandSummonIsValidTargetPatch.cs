using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// Vanilla <see cref="CardModel.IsValidTarget"/> returns false when <c>target == null</c> and
/// <see cref="CardModel.TargetType"/> is <see cref="TargetType.AnyEnemy"/> (or AnyAlly). YGO hand summons often enqueue
/// <see cref="MegaCrit.Sts2.Core.GameActions.PlayCardAction"/> without a pre-selected target (attack stance, or defense when
/// <see cref="BaseMonsterCard.NonAttackPlayTargetType"/> is AnyEnemy / AnyAlly). Allow null in hand so remote peers match.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
public static class YgoMonsterHandSummonIsValidTargetPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (target != null || __result)
            return;

        if (__instance is not BaseMonsterCard)
            return;

        if (__instance.Pile?.Type != PileType.Hand)
            return;

        TargetType tt = __instance.TargetType;
        if (tt == TargetType.AnyEnemy || tt == TargetType.AnyAlly)
            __result = true;
    }
}
