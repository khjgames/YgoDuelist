using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Shared post-play hooks for effect monsters that key off spell plays or defend command resolution.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class HookAfterCardPlayedEffectMonsterPatch
{
    [HarmonyPostfix]
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = combatState;
        _ = choiceContext;

        if (cardPlay.Card?.Owner is not Player player)
            return;

        if (cardPlay.Card is BaseSpellCard)
            RegisterSpellCounterOnFieldMonsters(player);

        if (cardPlay.Card is Command_Defend defend && defend.SourceMonster is IYgoDeferredBlockFromDefendCommand src)
        {
            int delayedBlock = src.GetDeferredBlockForDefendCommand();
            if (delayedBlock > 0)
                YgoTotalDefenseShogunDeferredBlock.Queue(player, delayedBlock);
        }

        await Task.CompletedTask;
    }

    private static void RegisterSpellCounterOnFieldMonsters(Player player)
    {
        foreach (BaseMonsterCard field in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (field is IYgoSpellCounterMonster spellCounterMonster)
                spellCounterMonster.AddSpellCounter(1);
        }
    }
}
