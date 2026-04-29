using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCommandSystem;

/// <summary>Advances 7 Weapons stacks for <see cref="YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.The_Hunter_with_7_Weapons"/> when the controlling player plays any card.</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class SevenWeaponsAfterCardPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = combatState;
        _ = choiceContext;
        _ = TaskHelper.RunSafely(PostfixAsync(cardPlay));
    }

    private static async Task PostfixAsync(CardPlay cardPlay)
    {
        if (cardPlay.Card?.Owner is not Player player)
            return;

        SevenWeaponsHunterState.RegisterCardPlayed(player);
        await SevenWeaponsHunterState.SyncHunterPetsAsync(player);
    }
}
