using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="Elephant_Statue_of_Blessing"/>: when sent from hand to the graveyard, draw 2 (3 if upgraded).
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
public static class HookAfterCardChangedPilesElephantStatueBlessingPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        Task __result,
        IRunState runState,
        CombatState? combatState,
        CardModel card,
        PileType oldPile,
        AbstractModel? source)
    {
        _ = runState;
        _ = source;
        if (__result == null)
            return;

        _ = RunAfterHookAsync(__result, combatState, card, oldPile);
    }

    private static async Task RunAfterHookAsync(Task hookTask, CombatState? combatState, CardModel card, PileType oldPile)
    {
        await hookTask;

        if (combatState == null || !CombatManager.Instance.IsInProgress)
            return;

        if (oldPile != PileType.Hand)
            return;

        if (card.Pile?.Type != GraveyardPile.CustomType)
            return;

        if (card is not Elephant_Statue_of_Blessing elephant || card.Owner == null)
            return;

        int n = elephant.IsUpgraded ? 3 : 2;
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), n, card.Owner);
    }
}
