using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Logical bridge for the combat Spell/Trap zone list used by second-hand UI.
/// </summary>
public static class YgoSpellTrapZoneBridge
{
    private static readonly Dictionary<Player, List<CardModel>> VisibleCardsByPlayer = new();

    public static IReadOnlyList<CardModel> GetVisibleCards(Player player)
    {
        if (VisibleCardsByPlayer.TryGetValue(player, out var cards))
            return cards;
        return Array.Empty<CardModel>();
    }

    public static void SyncFromZonePile(Player player)
    {
        var pile = SpellTrapZonePile.CustomType.GetPile(player);
        if (pile == null)
        {
            bool removed = VisibleCardsByPlayer.Remove(player);
            if (removed)
                YgoSecondHandSourceBridge.NotifySpellTrapZoneChanged(player, Array.Empty<CardModel>());
            return;
        }

        List<CardModel> ordered = pile.Cards
            .OrderByDescending(IsFieldSpell)
            .ToList();

        VisibleCardsByPlayer[player] = ordered;
        YgoSecondHandSourceBridge.NotifySpellTrapZoneChanged(player, ordered);
    }

    public static bool IsSpellOrTrapCard(CardModel card)
    {
        return card is IYgoCard ygo
               && (ygo.YgoCardType == YgoCardType.Spell || ygo.YgoCardType == YgoCardType.Trap);
    }

    public static bool IsFieldSpell(CardModel card)
    {
        return card is IYgoCard ygo && ygo.DuelMonsterRace == DuelMonsterRace.SpellField;
    }

    public static bool IsTrap(CardModel card)
    {
        return card is IYgoCard ygo && ygo.YgoCardType == YgoCardType.Trap;
    }

    public static bool IsInZone(CardModel card)
    {
        return card?.Pile?.Type == SpellTrapZonePile.CustomType;
    }

    public static int CountNonFieldCards(Player player)
    {
        var pile = SpellTrapZonePile.CustomType.GetPile(player);
        if (pile == null)
            return 0;
        return pile.Cards.Count(c => !IsFieldSpell(c));
    }

    public static bool HasSpaceForSetOrPlay(Player player, CardModel card)
    {
        if (IsFieldSpell(card))
            return true;
        return CountNonFieldCards(player) < 5;
    }

    public static async Task<bool> TrySetFromHandAsync(CardModel card)
    {
        if (card?.Owner == null)
            return false;
        if (card.Pile?.Type != PileType.Hand)
            return false;
        if (!IsSpellOrTrapCard(card))
            return false;

        Player player = card.Owner;
        CardPile? zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (zonePile == null)
            return false;

        if (IsFieldSpell(card))
        {
            CardModel? existingField = zonePile.Cards.FirstOrDefault(IsFieldSpell);
            if (existingField != null)
            {
                CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
                if (graveyard != null)
                {
                    await CardPileCmd.Add(
                        new[] { existingField },
                        graveyard,
                        CardPilePosition.Top,
                        card,
                        false);
                }
            }
        }
        else if (!HasSpaceForSetOrPlay(player, card))
        {
            return false;
        }

        switch (card)
        {
            case BaseFieldSpellCard fieldSpell:
                // Set from hand: face-down in the field slot. (Activate/play uses MarkAsFaceUpFieldInZone + ActivateFieldSpellFromHandAsync.)
                fieldSpell.EnterSpellTrapZoneAsSetCard();
                break;
            case BaseSpellCard s:
                s.EnterSpellTrapZoneAsSetCard();
                break;
            case BaseTrapCard t:
                t.EnterSpellTrapZoneAsSetCard();
                break;
        }

        await CardPileCmd.Add(
            new[] { card },
            zonePile,
            CardPilePosition.Top,
            card,
            false);

        SyncFromZonePile(player);
        if (card is BaseFieldSpellCard)
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
        return true;
    }

    /// <summary>
    /// Activates a field spell from the hand: replaces any existing field spell (sent to GY), then adds this card face-up to the zone.
    /// </summary>
    public static async Task ActivateFieldSpellFromHandAsync(BaseFieldSpellCard card)
    {
        if (card.Owner == null)
            return;

        Player player = card.Owner;
        CardPile? zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (zonePile == null)
            return;

        // Manual play from hand: OnPlayWrapper may move the card to PileType.Play before OnPlay runs.
        // Activation from the Spell/Trap row: card stays on SpellTrapZonePile until this method re-adds it face-up.
        PileType from = card.Pile?.Type ?? PileType.None;
        if (from != PileType.Hand && from != PileType.Play && from != SpellTrapZonePile.CustomType)
            return;

        card.MarkAsFaceUpFieldInZone();

        CardModel? existingField = zonePile.Cards.FirstOrDefault(IsFieldSpell);
        if (existingField != null && !ReferenceEquals(existingField, card))
        {
            CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
            if (graveyard != null)
            {
                await CardPileCmd.Add(
                    new[] { existingField },
                    graveyard,
                    CardPilePosition.Top,
                    card,
                    false);
            }
        }

        await CardPileCmd.Add(
            new[] { card },
            zonePile,
            CardPilePosition.Top,
            card,
            false);

        SyncFromZonePile(player);
    }

    /// <summary>
    /// From hand: moves the card into the Spell/Trap zone then attaches. From zone (set equip): card is already there—attach only.
    /// </summary>
    public static async Task ActivateEquipSpellAsync(BaseEquipSpellCard card, BaseMonsterCard targetMonster)
    {
        if (card.Owner == null)
            return;

        Player player = card.Owner;
        CardPile? zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (zonePile == null)
            return;

        PileType equipFrom = card.Pile?.Type ?? PileType.None;
        if (equipFrom == PileType.Hand || equipFrom == PileType.Play)
        {
            if (!HasSpaceForSetOrPlay(player, card))
                return;

            await CardPileCmd.Add(
                new[] { card },
                zonePile,
                CardPilePosition.Top,
                card,
                false);
        }
        else if (!ReferenceEquals(card.Pile, zonePile))
            return;

        YgoEquipSpellRegistry.Attach(card, targetMonster);

        SyncFromZonePile(player);
    }

    /// <summary>
    /// From hand/play: moves the card into the Spell/Trap zone. From zone (set continuous): already there—flip/sync only.
    /// </summary>
    public static async Task ActivateContinuousSpellAsync(BaseContinuousSpellCard card)
    {
        if (card.Owner == null)
            return;

        Player player = card.Owner;
        CardPile? zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (zonePile == null)
            return;

        PileType from = card.Pile?.Type ?? PileType.None;
        if (from != PileType.Hand && from != PileType.Play && from != SpellTrapZonePile.CustomType)
            return;

        if (from == PileType.Hand || from == PileType.Play)
        {
            if (!HasSpaceForSetOrPlay(player, card))
                return;

            await CardPileCmd.Add(
                new[] { card },
                zonePile,
                CardPilePosition.Top,
                card,
                false);
        }
        else if (!ReferenceEquals(card.Pile, zonePile))
            return;

        SyncFromZonePile(player);
    }
}
