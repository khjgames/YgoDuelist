using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(DieForYouPower), nameof(DieForYouPower.ModifyUnblockedDamageTarget))]
public static class DieForYouPowerBattleDamageDealerPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        DieForYouPower __instance,
        ValueProp props,
        ref Creature __result,
        object[] __args)
    {
        try
        {
            Creature pet = __instance.Owner;

            if (__result != pet)
                return;

            if (!props.IsPoweredAttack())
                return;

            Creature? dealer = __args.Length >= 4 ? __args[3] as Creature : null;
            if (dealer == null || dealer.Side != CombatSide.Enemy)
                return;

            MonsterCommandRegistry.GetOrCreate(pet).DieForYouRedirectedBattleDamageDealer = dealer;
        }
        catch
        {
            // Do not crash combat from this stamp patch.
        }
    }
}