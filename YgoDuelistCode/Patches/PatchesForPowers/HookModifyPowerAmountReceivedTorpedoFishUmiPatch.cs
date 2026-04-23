using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Face spell zone + <see cref="IYgoPetDebuffPowerAmountReceivedHook"/> (Torpedo Fish under Umi, etc.).
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

        if (!target.IsPet || target.PetOwner?.Creature == null)
            return;

        if (DuelMonsterFieldRegistry.GetSourceMonster<IYgoPetDebuffPowerAmountReceivedHook>(target) is not IYgoPetDebuffPowerAmountReceivedHook hook)
            return;

        hook.TryZeroIncomingDebuffPowerAmount(ref __result, combatState, canonicalPower, target, amount, giver);
    }
}
