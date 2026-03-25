using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// While <see cref="Umi"/> is face-up in your field spell zone, <see cref="Torpedo_Fish"/> pets ignore debuff powers.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountReceived))]
public static class HookModifyPowerAmountReceivedTorpedoFishUmiPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        ref decimal __result,
        CombatState combatState,
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? giver)
    {
        _ = combatState;
        _ = amount;
        _ = giver;
        if (__result == 0m)
            return;

        if (canonicalPower.GetTypeForAmount(__result) != PowerType.Debuff)
            return;

        if (!target.IsPet || target.PetOwner == null)
            return;

        if (DuelMonsterFieldRegistry.GetSourceCardForPet(target) is not Torpedo_Fish || target.PetOwner.Creature == null)
            return;

        if (!YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(target.PetOwner).Any(static fs => fs is Umi))
            return;

        __result = 0m;
    }
}
