using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// At end of every combat, clear YgoDuelist combat-scoped state so the next combat
/// does not see data from previous combats (e.g. CalcDuelMonsterStats using
/// old field monsters, command state, or normal-summon tracking).
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class YgoCombatEndClearPatch
{
    [HarmonyPostfix]
    public static async void Postfix(IRunState runState, CombatState? combatState, CombatRoom room)
    {
        DuelMonsterFieldRegistry.ClearAll();
        MonsterCommandRegistry.ClearAll();
        NormalSummonTracker.ClearAll();
        TributeMaterialMarkTracker.ClearAll();
        await Task.CompletedTask;
    }
}
