using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Powers;
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
        if (combatState != null)
        {
            var ctx = new BlockingPlayerChoiceContext();
            foreach (Player p in combatState.Players)
                await RaDoomedPower.ResolveCombatEndDamageAsync(ctx, p);
        }

        DuelMonsterFieldRegistry.ClearAll();
        YgoDuelistPassivePowerState.ClearAll();
        MonsterCommandRegistry.ClearAll();
        YgoDarkSpiritSilentState.ClearAll();
        NormalSummonTracker.ClearAll();
        LegionFiendJesterSpellcasterConduit.ClearAll();
        TributeSummonPlayPayload.ClearAll();
        EquipSpellPlayPayload.ClearAll();
        RitualSpellPlayPayload.ClearAll();
        FusionSpellPlayPayload.ClearAll();
        TailorOfTheFicklePlayPayload.ClearAll();
        EmergencyProvisionsPlayPayload.ClearAll();
        ActivatedEffectTributeSelectionPayload.ClearAll();
        ObeliskActivatedTributePayload.ClearAll();
        RushReliablePlayPayload.ClearAll();
        RiryokuPlayPayload.ClearAll();
        SecretPassPlayPayload.ClearAll();
        FairyOfSpringReturnedEquipLock.ClearAll();
        YgoEquipSpellRegistry.ClearAll();
        YgoSpellTrapEquipLinkRegistry.ClearAll();
        YgoCurseOfDarknessSpellHook.ClearAll();
        YgoDesCounterblowThornsSync.ClearAll();

        if (combatState != null)
        {
            foreach (var p in combatState.Players)
                await YgoShadowRealmService.RemoveAllFromCombat(p);
        }

        await Task.CompletedTask;
    }
}
