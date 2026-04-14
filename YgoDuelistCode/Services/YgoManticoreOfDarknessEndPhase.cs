using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// End of your turn (before hand discard flush): if <see cref="Manticore_of_Darkness"/> was sent to your Graveyard this turn,
/// you may send 1 Beast / Beast-Warrior / Winged Beast from your hand or field to the Graveyard to Special Summon that Manticore.
/// </summary>
public static class YgoManticoreOfDarknessEndPhase
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-MANTICORE_OF_DARKNESS.activate_effect");
    private static readonly LocString FodderPrompt = new("cards", "YGODUELIST-MANTICORE_OF_DARKNESS.select_fodder");

    private sealed class Pending
    {
        public required Player Player { get; init; }
        public required Manticore_of_Darkness Card { get; init; }
        public int SentTurnStamp { get; init; }
    }

    private static readonly List<Pending> Pendings = new();

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Manticore_of_Darkness manticore)
            return;
        if (pile.Type != GraveyardPile.CustomType || !pile.IsCombatPile)
            return;
        if (CombatManager.Instance is not { IsInProgress: true })
            return;
        CombatState? cs = CombatManager.Instance.DebugOnlyGetState();
        if (cs == null)
            return;

        Player? player = ResolveGraveyardOwner(cs, pile) ?? addedCard.Owner;
        if (player == null)
            return;

        Pendings.Add(new Pending
        {
            Player = player,
            Card = manticore,
            SentTurnStamp = YgoPlayerCombatTurnStamp.Get(player)
        });
    }

    public static async Task TryResolveBeforePlayerTurnEndFlushAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature?.CombatState == null)
            return;

        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        int currentStamp = YgoPlayerCombatTurnStamp.Get(player);
        CardPile? gy = GraveyardRelic.GetGraveyardPile(player);
        if (gy == null)
            return;

        for (int i = Pendings.Count - 1; i >= 0; i--)
        {
            Pending p = Pendings[i];
            if (p.Player != player)
                continue;
            if (p.SentTurnStamp != currentStamp || !gy.Cards.Contains(p.Card))
            {
                Pendings.RemoveAt(i);
                continue;
            }

            await TryResolveOneAsync(ctx, player, p.Card, gy);
            Pendings.RemoveAt(i);
        }
    }

    private static async Task TryResolveOneAsync(
        PlayerChoiceContext ctx,
        Player player,
        Manticore_of_Darkness manticoreInGy,
        CardPile gy)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> fodder = BuildFodder(player, manticoreInGy);
        if (fodder.Count == 0)
            return;

        var activatePrefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> activationPick;
        try
        {
            activationPick = await CardSelectCmd.FromSimpleGrid(ctx, new[] { manticoreInGy }, player, activatePrefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (activationPick.FirstOrDefault() is not Manticore_of_Darkness)
            return;

        fodder = BuildFodder(player, manticoreInGy);
        if (fodder.Count == 0)
            return;

        IEnumerable<CardModel> fodderPick;
        try
        {
            fodderPick = await CardSelectCmd.FromSimpleGrid(
                ctx,
                fodder,
                player,
                new CardSelectorPrefs(FodderPrompt, 1, 1) { Cancelable = true });
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (fodderPick.FirstOrDefault() is not BaseMonsterCard food)
            return;

        if (!await SendMonsterToGraveyardAsync(player, food))
            return;

        if (!gy.Cards.Contains(manticoreInGy))
            return;

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, manticoreInGy, ctx))
            return;
    }

    private static List<BaseMonsterCard> BuildFodder(Player player, Manticore_of_Darkness selfInGy)
    {
        var list = new List<BaseMonsterCard>();
        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand != null)
        {
            foreach (CardModel c in hand.Cards)
            {
                if (c is BaseMonsterCard bm && IsFodderRace(bm.DuelMonsterRace))
                    list.Add(bm);
            }
        }

        foreach (BaseMonsterCard field in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (IsFodderRace(field.DuelMonsterRace))
                list.Add(field);
        }

        return list;
    }

    private static bool IsFodderRace(DuelMonsterRace r) =>
        r == DuelMonsterRace.Beast || r == DuelMonsterRace.BeastWarrior || r == DuelMonsterRace.WingedBeast;

    private static async Task<bool> SendMonsterToGraveyardAsync(Player player, BaseMonsterCard m)
    {
        CardPile? gy = GraveyardRelic.GetGraveyardPile(player);
        if (gy == null)
            return false;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand != null && m.Pile == hand)
        {
            await CardPileCmd.Add(new[] { m }, gy, CardPilePosition.Top, m, false);
            return true;
        }

        if (m is not NormalMonsterCard nm)
            return false;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(nm, player);
        if (pet == null)
            return false;

        await CreatureCmd.Kill(pet, force: true);
        await CardPileCmd.Add(new[] { m }, gy, CardPilePosition.Top, m, false);
        return true;
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

    public static void ClearAll() => Pendings.Clear();
}
