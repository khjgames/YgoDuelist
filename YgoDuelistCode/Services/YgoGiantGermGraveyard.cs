using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Giant_Germ"/>: when destroyed by battle and sent to the Graveyard, inflict <c>Mgc</c> Blight on all enemies,
/// then optionally Special Summon up to 2 <see cref="Giant_Germ"/> from the deck.
/// </summary>
public static class YgoGiantGermGraveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-GIANT_GERM.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-GIANT_GERM.summon_from_deck");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Giant_Germ germ)
            return;
        if (!YgoBattleDeathMarkedCards.Consume(germ))
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        TaskHelper.RunSafely(RunAsync(player, germ));
    }

    private static List<CardModel> BuildDeckCandidates(Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards)
            .OfType<Giant_Germ>()
            .Where(g => g.CanSummonDuelMonster && ReactorSlimeSummonGate.AllowsSummon(player, g))
            .Cast<CardModel>()
            .ToList();
    }

    private static async Task RunAsync(Player player, Giant_Germ sourceInGraveyard)
    {
        int blight = (int)sourceInGraveyard.DynamicVars["Mgc"].BaseValue;
        if (blight > 0 && player.Creature?.CombatState != null)
        {
            foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(player.Creature.CombatState))
                await PowerCmd.Apply<BlightPower>(enemy, blight, player.Creature, sourceInGraveyard);
        }

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            sourceInGraveyard,
            ActivatePrompt);
        if (ctx == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                return;

            List<CardModel> germs = BuildDeckCandidates(player);
            if (germs.Count == 0)
                return;

            var summonPrefs = new CardSelectorPrefs(SummonPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            List<Giant_Germ> BuildTypedDeckCandidates() =>
                BuildDeckCandidates(player).OfType<Giant_Germ>().ToList();

            Giant_Germ? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                summonPrefs,
                BuildTypedDeckCandidates);
            if (chosen == null)
                return;

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
        }
    }
}
