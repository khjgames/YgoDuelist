using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// While <see cref="DustBarrierFieldPower"/> is active, player Normal Monster pets ignore Weak, Frail, and negative Strength/Dexterity.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountReceived))]
public static class HookModifyPowerAmountReceivedDustBarrierPatch
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

        if (!target.IsPet || target.PetOwner == null)
            return;

        if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(target) is not BaseMonsterCard bm || bm.YgoCardType != YgoCardType.Monster)
            return;

        Creature? hero = target.PetOwner.Creature;
        DustBarrierFieldPower? dust = hero?.GetPower<DustBarrierFieldPower>();
        if (dust == null || dust.Amount <= 0m)
            return;

        switch (canonicalPower)
        {
            case WeakPower when __result > 0m:
            case FrailPower when __result > 0m:
                __result = 0m;
                return;
            case StrengthPower when __result < 0m:
            case DexterityPower when __result < 0m:
                __result = 0m;
                return;
        }
    }
}
