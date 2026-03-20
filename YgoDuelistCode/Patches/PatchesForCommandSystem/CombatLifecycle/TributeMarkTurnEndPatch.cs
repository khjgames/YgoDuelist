using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeTurnEnd))]
public static class TributeMarkTurnEndPatch
{
    [HarmonyPostfix]
    public static async void Postfix(CombatState combatState, CombatSide side)
    {
        if (combatState == null || side != CombatSide.Player)
            return;

        foreach (Player player in combatState.Players)
            await TributeMaterialMarkTracker.ClearForPlayerAsync(player);

        await Task.CompletedTask;
    }
}
