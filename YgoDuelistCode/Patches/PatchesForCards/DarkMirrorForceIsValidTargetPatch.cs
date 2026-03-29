using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// <see cref="Dark_Mirror_Force"/> must target an enemy whose current move includes player-directed attack damage (same basis as <see cref="YgoIntentAttackDamage"/>).
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
public static class DarkMirrorForceIsValidTargetPatch
{
    static void Postfix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (!__result || __instance is not Dark_Mirror_Force || target == null)
            return;

        Creature? playerCreature = __instance.Owner?.Creature;
        if (playerCreature == null)
        {
            __result = false;
            return;
        }

        __result = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, playerCreature) > 0;
    }
}
