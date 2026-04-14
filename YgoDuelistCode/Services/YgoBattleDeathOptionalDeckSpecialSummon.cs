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
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Optional activate in GY, then Special Summon from deck — shared by Mother Grizzly, Shining Angel, Pyramid Turtle, Mystic Tomato.</summary>
public static class YgoBattleDeathOptionalDeckSpecialSummon
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not BaseMonsterCard source)
            return;
        if (source is not IBattleDeathOptionalDeckSpecialSummon effect)
            return;
        if (!YgoBattleDeathMarkedCards.Consume(source))
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

        TaskHelper.RunSafely(RunAsync(player, source, effect));
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

    private static List<BaseMonsterCard> CollectCandidates(Player player, IBattleDeathOptionalDeckSpecialSummon effect)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        if (draw == null)
            return [];

        return draw.Cards
            .OfType<BaseMonsterCard>()
            .Where(effect.IsBattleDeathDeckSummonCandidate)
            .ToList();
    }

    private static async Task RunAsync(Player player, BaseMonsterCard source, IBattleDeathOptionalDeckSpecialSummon effect)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> candidates = CollectCandidates(player, effect);
        if (candidates.Count == 0)
            return;

        var ctx = new BlockingPlayerChoiceContext();

        var activatePrefs = new CardSelectorPrefs(effect.BattleDeathActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> activationPick = await CardSelectCmd.FromSimpleGrid(
            ctx,
            new[] { source },
            player,
            activatePrefs);

        if (activationPick.FirstOrDefault() != source)
            return;

        candidates = CollectCandidates(player, effect);
        if (candidates.Count == 0)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        var summonPrefs = new CardSelectorPrefs(effect.BattleDeathSummonPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> summonPick = await CardSelectCmd.FromSimpleGrid(ctx, candidates, player, summonPrefs);
        if (summonPick.FirstOrDefault() is not BaseMonsterCard chosen)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }
}
