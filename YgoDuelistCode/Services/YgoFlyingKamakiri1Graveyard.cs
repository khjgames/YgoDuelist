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
/// <see cref="Flying_Kamakiri_1"/>: when sent to the Graveyard, optional activation then Special Summon 1 WIND monster with printed ATK 15 or less from your deck.
/// </summary>
public static class YgoFlyingKamakiri1Graveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-FLYING_KAMAKIRI_1.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-FLYING_KAMAKIRI_1.summon_wind");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Flying_Kamakiri_1 km)
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

        TaskHelper.RunSafely(RunAsync(player, km));
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

    private static List<BaseMonsterCard> CollectWindDeckCandidates(Player player)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        if (draw == null)
            return [];

        var list = new List<BaseMonsterCard>();
        foreach (CardModel c in draw.Cards)
        {
            if (c is not BaseMonsterCard m)
                continue;
            if (m.DuelMonsterAttribute != DuelMonsterAttribute.Wind)
                continue;
            if (m.BaseAtk > 15)
                continue;
            if (!m.CanSummonDuelMonster)
                continue;
            list.Add(m);
        }

        return list;
    }

    private static async Task RunAsync(Player player, Flying_Kamakiri_1 sourceInGraveyard)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> candidates = CollectWindDeckCandidates(player);
        if (candidates.Count == 0)
            return;

        var ctx = new BlockingPlayerChoiceContext();

        var activatePrefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> activationPick = await CardSelectCmd.FromSimpleGrid(
            ctx,
            new[] { sourceInGraveyard },
            player,
            activatePrefs);

        if (activationPick.FirstOrDefault() is not Flying_Kamakiri_1)
            return;

        candidates = CollectWindDeckCandidates(player);
        if (candidates.Count == 0)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        var summonPrefs = new CardSelectorPrefs(SummonPrompt, 1, 1)
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
