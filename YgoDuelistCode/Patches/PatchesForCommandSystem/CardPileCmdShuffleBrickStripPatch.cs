using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCommandSystem;

/// <summary>
/// After the discard pile is shuffled into the draw pile, eject <see cref="Cards.Core.IYgoBrickCard"/> instances back to discard.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle))]
public static class CardPileCmdShuffleBrickStripPatch
{
    [HarmonyPostfix]
    public static void Postfix(Task __result, PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;
        if (player.PlayerCombatState == null)
            return;
        _ = TaskHelper.RunSafely(AfterShuffleAsync(__result, player));
    }

    private static async Task AfterShuffleAsync(Task shuffleTask, Player player)
    {
        await shuffleTask;
        YgoBrickCardBootstrap.StripBricksFromPlayerCombatPiles(player);
    }
}
