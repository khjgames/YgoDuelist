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
using YgoDuelist.YgoDuelistCode.Cards.Core;

using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Heads/Tails call + flip; on match, apply this trap's <c>Mgc</c> stacks of Weak to all enemies. RNG salt distinguishes activation vs start-of-turn.
/// </summary>
public static class YgoFairyBoxHeadsTailsWeak
{
    public static Task RunImmediateActivationAsync(
        PlayerChoiceContext choiceContext,
        CombatState cs,
        Player player,
        Creature ownerCreature,
        BaseTrapCard trapCard) =>
        RunCoinCallFlipAndWeakAsync(choiceContext, cs, player, ownerCreature, trapCard, "ACTIVATE");

    public static Task RunStartOfYourTurnAsync(
        PlayerChoiceContext choiceContext,
        CombatState cs,
        Player player,
        Creature ownerCreature,
        BaseTrapCard trapCard) =>
        RunCoinCallFlipAndWeakAsync(choiceContext, cs, player, ownerCreature, trapCard, "TURN_START");

    private static async Task RunCoinCallFlipAndWeakAsync(
        PlayerChoiceContext choiceContext,
        CombatState cs,
        Player player,
        Creature ownerCreature,
        BaseTrapCard trapCard,
        string coinSaltSegment)
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
        string coinKey = "FAIRY_BOX-COIN-" + coinSaltSegment;
        bool flipIsHeads = YgoDeterministicRng.CoinFlip(cs, coinKey, YgoDeterministicRng.MixSpellTrapZoneSlot(player, trapCard));

        CardModel resultCard = YgoDeterministicRngResultDisplay.CreateCoinFlipResultCard(cs, player, flipIsHeads);
        var coinPrompt = new LocString("cards", "YGODUELIST-FAIRY_BOX.coin_result.selection");
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new List<CardModel> { resultCard }, player, coinPrompt);

        if (calledHeads != flipIsHeads)
            return;

        decimal weakStacks = trapCard.DynamicVars["Mgc"].BaseValue;
        foreach (Creature e in YgoMpCombatOrder.CreatureListOrderedByCombatId(cs.HittableEnemies))
        {
            if (e.IsAlive)
                await PowerCmd.Apply<WeakPower>(e, weakStacks, ownerCreature, trapCard);
        }
    }
}
