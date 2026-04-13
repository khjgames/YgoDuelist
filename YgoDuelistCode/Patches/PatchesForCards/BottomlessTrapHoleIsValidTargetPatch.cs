using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// <see cref="Bottomless_Trap_Hole"/> may only target an enemy whose current attack intent vs the player is at least this card's threshold (<c>Mgc</c>), matching <see cref="Bottomless_Trap_Hole.OnTrapPlay"/>.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
public static class BottomlessTrapHoleIsValidTargetPatch
{
    static void Postfix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (!__result || __instance is not Bottomless_Trap_Hole bth || target == null)
            return;

        Creature? playerCreature = __instance.Owner?.Creature;
        if (playerCreature == null)
        {
            __result = false;
            return;
        }

        decimal threshold = bth.DynamicVars["Mgc"].BaseValue;
        int incoming = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, playerCreature);
        __result = (decimal)incoming >= threshold;
    }
}
