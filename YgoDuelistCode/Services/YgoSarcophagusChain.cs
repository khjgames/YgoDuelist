using System.Collections.Generic;
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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Continuous Trap <see cref="The_First_Sarcophagus"/>: turn-start placement, trio completion into <see cref="Spirit_of_the_Pharaoh"/>,
/// and linked destruction when any piece leaves the field.
/// </summary>
public static class YgoSarcophagusChain
{
    private static int _suppressLinkedDestructionDepth;

    public static bool IsSarcophagusPiece(CardModel? card) =>
        card is The_First_Sarcophagus or The_Second_Sarcophagus or The_Third_Sarcophagus;

    public static void EnterLinkedDestructionSuppress() => _suppressLinkedDestructionDepth++;

    public static void ExitLinkedDestructionSuppress()
    {
        if (_suppressLinkedDestructionDepth > 0)
            _suppressLinkedDestructionDepth--;
    }

    public static bool IsLinkedDestructionSuppressed => _suppressLinkedDestructionDepth > 0;

    public static async Task OnSarcophagusPieceRemovedFromFieldAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel removedPiece)
    {
        if (IsLinkedDestructionSuppressed || !IsSarcophagusPiece(removedPiece))
            return;

        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return;

        List<CardModel> others = zone.Cards.Where(c => IsSarcophagusPiece(c) && !ReferenceEquals(c, removedPiece)).ToList();
        foreach (CardModel c in others)
        {
            await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(
                c,
                removedPiece,
                YgoDestructionSourceKind.TrapEffect);
        }

        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }

    public static async Task ResolveTurnStartAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        The_First_Sarcophagus first)
    {
        if (player.PlayerCombatState == null || player.Creature == null)
            return;

        if (await TryResolveTrioSpecialSummonSpiritAsync(choiceContext, player, first))
            return;

        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return;

        if (YgoSpellTrapZoneBridge.CountNonFieldCards(player) >= 5)
            return;

        bool secondUp = zone.Cards.Any(c => c is The_Second_Sarcophagus s && !s.FaceDown);
        if (!secondUp)
        {
            if (await TryPlaceContinuousFromHandOrDeckAsync<The_Second_Sarcophagus>(choiceContext, player, first))
                return;
        }

        if (YgoSpellTrapZoneBridge.CountNonFieldCards(player) >= 5)
            return;

        zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return;

        bool thirdUp = zone.Cards.Any(c => c is The_Third_Sarcophagus t && !t.FaceDown);
        if (!thirdUp)
            await TryPlaceContinuousFromHandOrDeckAsync<The_Third_Sarcophagus>(choiceContext, player, first);
    }

    private static bool ZoneHasFaceUpSarcophagusTrio(Player player)
    {
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return false;
        bool hasFirst = zone.Cards.Any(c => c is The_First_Sarcophagus f && !f.FaceDown);
        bool hasSecond = zone.Cards.Any(c => c is The_Second_Sarcophagus s && !s.FaceDown);
        bool hasThird = zone.Cards.Any(c => c is The_Third_Sarcophagus t && !t.FaceDown);
        return hasFirst && hasSecond && hasThird;
    }

    private static async Task<bool> TryResolveTrioSpecialSummonSpiritAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        The_First_Sarcophagus first)
    {
        if (!ZoneHasFaceUpSarcophagusTrio(player))
            return false;

        List<Spirit_of_the_Pharaoh> spiritPool = BuildSpiritHandOrDeckCandidates(player);
        if (spiritPool.Count == 0 || !DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;

        Spirit_of_the_Pharaoh? spirit = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(
                new LocString("cards", "YGODUELIST-THE_FIRST_SARCOPHAGUS.pick_spirit"),
                1,
                1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildSpiritHandOrDeckCandidates(player));
        if (spirit == null || !spiritPool.Contains(spirit))
            return false;

        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return false;

        List<CardModel> trio = zone.Cards.Where(IsSarcophagusPiece).ToList();
        if (trio.Count < 3)
            return false;

        EnterLinkedDestructionSuppress();
        try
        {
            foreach (CardModel piece in trio)
            {
                await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(
                    piece,
                    first,
                    YgoDestructionSourceKind.TrapEffect);
            }
        }
        finally
        {
            ExitLinkedDestructionSuppress();
        }

        YgoSpellTrapZoneBridge.SyncFromZonePile(player);

        YgoSpiritOfThePharaohSummonGate.Enter();
        bool ok;
        try
        {
            ok = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, spirit, choiceContext);
        }
        finally
        {
            YgoSpiritOfThePharaohSummonGate.Exit();
        }

        return ok;
    }

    public static List<Spirit_of_the_Pharaoh> BuildSpiritHandOrDeckCandidates(Player player)
    {
        var list = new List<Spirit_of_the_Pharaoh>();
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand != null)
            list.AddRange(YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards).OfType<Spirit_of_the_Pharaoh>());
        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw != null)
            list.AddRange(YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards).OfType<Spirit_of_the_Pharaoh>());
        return list;
    }

    private static async Task<bool> TryPlaceContinuousFromHandOrDeckAsync<TContinuous>(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel effectSource)
        where TContinuous : BaseContinuousSpellCard
    {
        TContinuous? pick = FindFirstInHandOrDeckOrdered<TContinuous>(player);
        if (pick == null)
            return false;

        if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(player, pick))
            return false;

        YgoFirstSarcophagusPlacementGate.Enter();
        try
        {
            pick.YgoPrepareFaceUpContinuousFromCardEffect();
            CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
            if (zone == null)
                return false;

            await CardPileCmd.Add(new[] { pick }, zone, CardPilePosition.Top, effectSource, false);
            YgoSpellTrapZoneBridge.SyncFromZonePile(player);
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
            YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
            return true;
        }
        finally
        {
            YgoFirstSarcophagusPlacementGate.Exit();
        }
    }

    private static TContinuous? FindFirstInHandOrDeckOrdered<TContinuous>(Player player)
        where TContinuous : BaseContinuousSpellCard
    {
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand != null)
        {
            foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards))
            {
                if (c is TContinuous t)
                    return t;
            }
        }

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw != null)
        {
            foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards))
            {
                if (c is TContinuous t)
                    return t;
            }
        }

        return null;
    }
}
