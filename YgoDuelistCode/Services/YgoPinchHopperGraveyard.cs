using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Pinch_Hopper"/>: when sent to the Graveyard, optional activation (shows this card), then Special Summon 1 Insect monster from the hand.
/// </summary>
public static class YgoPinchHopperGraveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-PINCH_HOPPER.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-PINCH_HOPPER.summon_insect");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Pinch_Hopper pinch)
            return;
        if (pile.Type != GraveyardPile.CustomType || !pile.IsCombatPile)
            return;
        if (CombatManager.Instance is not { IsInProgress: true })
            return;
        CombatState? cs = CombatManager.Instance.DebugOnlyGetState();
        if (cs == null)
            return;

        Player? player = ResolveGraveyardOwner(cs, pile) ?? addedCard.Owner;
        if (player?.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
            return;

        TaskHelper.RunSafely(RunAsync(player, pinch));
    }

    private static Player? ResolveGraveyardOwner(CombatState cs, CardPile pile)
    {
        foreach (Player p in cs.Players)
        {
            if (GraveyardRelic.GetGraveyardPile(p) == pile)
                return p;
        }

        return null;
    }

    private static List<BaseMonsterCard> CollectInsectMonstersInHand(Player player)
    {
        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return [];

        return hand.Cards
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Insect && m.CanSummonDuelMonster)
            .ToList();
    }

    private static async Task RunAsync(Player player, Pinch_Hopper pinch)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> insects = CollectInsectMonstersInHand(player);
        if (insects.Count == 0)
            return;

        if (pinch.Pile?.Type != GraveyardPile.CustomType)
            return;

        var ctx = new BlockingPlayerChoiceContext();

        var activatePrefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> activationPick = await CardSelectCmd.FromSimpleGrid(
            ctx,
            new[] { pinch },
            player,
            activatePrefs);

        if (activationPick.FirstOrDefault() is not Pinch_Hopper)
            return;

        insects = CollectInsectMonstersInHand(player);
        if (insects.Count == 0)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        var summonPrefs = new CardSelectorPrefs(SummonPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> summonPick = await CardSelectCmd.FromSimpleGrid(ctx, insects, player, summonPrefs);
        if (summonPick.FirstOrDefault() is not BaseMonsterCard chosen)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }
}
