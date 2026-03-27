using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Once per turn, after you pay Fairy Box upkeep (Take 5 Damage): call Heads or Tails; if correct, apply 1 Weak to all enemies.
/// </summary>
public static class YgoFairyBoxHeadsTailsWeak
{
    public static async Task RunAfterUpkeepPaidAsync(
        PlayerChoiceContext choiceContext,
        CombatState cs,
        Player player,
        Creature ownerCreature,
        Fairy_Box trapCard)
    {
        CardModel headsCall = cs.CreateCard<Heads>(player);
        CardModel tailsCall = cs.CreateCard<Tails>(player);
        var coinOptions = new List<CardModel> { headsCall, tailsCall };

        CardModel? callPick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            coinOptions,
            player,
            canSkip: false);

        if (callPick == null)
            return;

        bool calledHeads = callPick.Id.Entry == headsCall.Id.Entry;
        bool flipIsHeads = YgoDeterministicRng.CoinFlip(cs, "FAIRY_BOX-COIN", YgoDeterministicRng.MixSpellTrapZoneSlot(player, trapCard));

        CardModel resultCard = YgoDeterministicRngResultDisplay.CreateCoinFlipResultCard(cs, player, flipIsHeads);
        var coinPrompt = new LocString("cards", "YGODUELIST-FAIRY_BOX.coin_result.selection");
        var coinPrefs = new CardSelectorPrefs(coinPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await CardSelectCmd.FromSimpleGrid(choiceContext, new List<CardModel> { resultCard }, player, coinPrefs);

        if (calledHeads != flipIsHeads)
            return;

        foreach (Creature e in cs.HittableEnemies)
        {
            if (e.IsAlive)
                await PowerCmd.Apply<WeakPower>(e, 1m, ownerCreature, trapCard);
        }
    }
}
