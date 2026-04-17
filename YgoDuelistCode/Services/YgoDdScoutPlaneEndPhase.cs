using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// D.D. Scout Plane: End Phase optional Special Summon if banished this turn.
/// </summary>
public static class YgoDdScoutPlaneEndPhase
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-D_D_SCOUT_PLANE.activate_return");

    public static void OnAddedToBanishedPile(Player player, IYgoDdScoutPlaneCard plane)
    {
        if (player == null)
            return;
        plane.MarkBanishedThisOwnerTurn(YgoPlayerCombatTurnStamp.Get(player));
    }

    public static void ClearTurnUsedFlags(Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        void scan(CardPile? pile)
        {
            if (pile == null)
                return;
            foreach (CardModel c in pile.Cards)
            {
                if (c is IYgoDdScoutPlaneCard d)
                    d.DdScoutEndPhaseUsedThisTurn = false;
            }
        }

        scan(PileType.Draw.GetPile(player));
        scan(PileType.Hand.GetPile(player));
        scan(PileType.Discard.GetPile(player));
        scan(GraveyardPile.CustomType.GetPile(player));
        scan(MonsterPile.CustomType.GetPile(player));
        scan(BanishedPile.CustomType.GetPile(player));
    }

    public static async Task TryResolveBeforePlayerTurnEndFlushAsync(PlayerChoiceContext choiceContext, Player player)
    {
        CardPile? banished = YgoBanishedService.GetPile(player);
        if (banished == null)
            return;

        int stamp = YgoPlayerCombatTurnStamp.Get(player);
        foreach (IYgoDdScoutPlaneCard planeCard in banished.Cards.OfType<IYgoDdScoutPlaneCard>().ToList())
        {
            if (!planeCard.IsBanishedThisTurnForEndPhase(stamp) || planeCard.DdScoutEndPhaseUsedThisTurn)
                continue;

            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                return;

            var prefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            if (planeCard is not CardModel cardModel)
                continue;

            var pick = await CardSelectCmd.FromSimpleGrid(choiceContext, new[] { cardModel }, player, prefs);
            if (pick.FirstOrDefault() is not IYgoDdScoutPlaneCard)
                continue;

            if (!banished.Cards.Contains(cardModel))
                continue;

            planeCard.DdScoutEndPhaseUsedThisTurn = true;
            await CardPileCmd.RemoveFromCombat(cardModel, false);
            if (planeCard is BaseMonsterCard bm)
                await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, bm, choiceContext);
        }
    }
}
