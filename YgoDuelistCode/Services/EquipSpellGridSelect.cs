using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Equip spell target grid: same MP contract as <see cref="TributeSummonGridSelect"/> — only the acting player runs UI;
/// other peers block on <see cref="PlayerChoiceSynchronizer.WaitForRemoteChoice"/> so <see cref="EquipSpellPlayPayload"/>
/// is set identically before <see cref="Cards.Core.BaseEquipSpellCard.OnPlay"/>.
/// </summary>
public static class EquipSpellGridSelect
{
    private static bool ShouldSelectLocalCard(Player player) =>
        LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;

    public static async Task<IEnumerable<CardModel>> FromSimpleGrid(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cardsIn,
        Player player,
        CardSelectorPrefs prefs)
    {
        if (CombatManager.Instance.IsEnding)
            return Array.Empty<CardModel>();

        List<CardModel> cards = cardsIn.ToList();
        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
            return cards.ToList();

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
        try
        {
            List<CardModel> result;
            if (ShouldSelectLocalCard(player))
            {
                if (CardSelectCmd.Selector != null)
                {
                    try
                    {
                        result = (await CardSelectCmd.Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect)).ToList();
                    }
                    catch (OperationCanceledException)
                    {
                        SyncCancelIfMp(player, choiceId);
                        throw;
                    }
                }
                else
                {
                    NPlayerHand.Instance?.CancelAllCardPlay();
                    NSimpleCardSelectScreen screen = NSimpleCardSelectScreen.Create(cards, prefs);
                    NOverlayStack.Instance.Push(screen);
                    try
                    {
                        result = (await screen.CardsSelected()).ToList();
                    }
                    catch (OperationCanceledException)
                    {
                        SyncCancelIfMp(player, choiceId);
                        throw;
                    }
                }

                List<int> indexes = result.Select(c => cards.IndexOf(c)).ToList();
                RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndexes(indexes));
            }
            else
            {
                result = (from i in (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndexes()
                    select cards[i]).ToList();
            }

            return result;
        }
        finally
        {
            await context.SignalPlayerChoiceEnded();
        }
    }

    private static void SyncCancelIfMp(Player player, uint choiceId)
    {
        if (!LocalContext.IsMe(player))
            return;
        if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            return;

        GD.Print($"[YgoDuelist][MP][Equip] Grid cancel → SyncLocalChoice empty indexes choiceId={choiceId} ownerNet={player.NetId}");
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
            player,
            choiceId,
            PlayerChoiceResult.FromIndexes(new List<int>()));
    }
}
