using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Spell/Trap zone targets for <c>FLIP</c> effects that destroy field cards.</summary>
public static class YgoFlipSpellTrapFieldEffects
{
    public static void CollectSpellsInAllSpellTrapZones(CombatState cs, List<BaseSpellCard> into)
    {
        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            CardPile? zone = YgoPlayerPiles.SpellTrapZone(p);
            if (zone == null)
                continue;
            foreach (CardModel c in zone.Cards)
            {
                if (c is BaseSpellCard s)
                    into.Add(s);
            }
        }
    }

    public static void CollectTrapsInAllSpellTrapZones(CombatState cs, List<BaseTrapCard> into)
    {
        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            CardPile? zone = YgoPlayerPiles.SpellTrapZone(p);
            if (zone == null)
                continue;
            foreach (CardModel c in zone.Cards)
            {
                if (c is BaseTrapCard t)
                    into.Add(t);
            }
        }
    }

    /// <summary>Set Spell/Trap Cards (facedown in the zone).</summary>
    public static void CollectSetSpellTrapsInAllSpellTrapZones(CombatState cs, List<CardModel> into)
    {
        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            CardPile? zone = YgoPlayerPiles.SpellTrapZone(p);
            if (zone == null)
                continue;
            foreach (CardModel c in zone.Cards)
            {
                switch (c)
                {
                    case BaseSpellCard s when s.FaceDown:
                        into.Add(s);
                        break;
                    case BaseTrapCard t when t.FaceDown:
                        into.Add(t);
                        break;
                }
            }
        }
    }

    public static async Task<bool> TrySendSpellTrapOnFieldToGraveyardAsync(
        CardModel spellOrTrapOnField,
        CardModel effectSourceCard)
    {
        Player? owner = spellOrTrapOnField.Owner;
        if (owner == null)
            return false;
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (gy == null)
            return false;
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(owner);
        if (zone == null || !zone.Cards.Contains(spellOrTrapOnField))
            return false;

        await CardPileCmd.Add(
            new[] { spellOrTrapOnField },
            gy,
            CardPilePosition.Top,
            effectSourceCard,
            false);
        return true;
    }

    /// <summary>
    /// Returns a spell/trap from the owner's spell/trap zone to their hand. Equip-link registry entries are
    /// <see cref="YgoSpellTrapEquipLinkRegistry.Detach"/> only (linked monster stays on the field).
    /// </summary>
    public static async Task<bool> TryReturnSpellTrapFromZoneToHandAsync(
        Player player,
        CardModel spellOrTrapOnField,
        CardModel effectSourceCard)
    {
        if (spellOrTrapOnField is not BaseSpellCard and not BaseTrapCard)
            return false;
        Player? owner = spellOrTrapOnField.Owner;
        if (owner == null || owner != player)
            return false;
        CardPile? hand = YgoPlayerPiles.Hand(owner);
        if (hand == null)
            return false;
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(owner);
        if (zone == null || !zone.Cards.Contains(spellOrTrapOnField))
            return false;

        YgoSpellTrapEquipLinkRegistry.Detach(spellOrTrapOnField);

        if (spellOrTrapOnField.Pile != hand)
        {
            await CardPileCmd.Add(
                new[] { spellOrTrapOnField },
                hand,
                CardPilePosition.Top,
                effectSourceCard,
                false);
        }

        YgoSpellTrapZoneBridge.SyncFromZonePile(owner);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(owner);
        return true;
    }

    public static void CollectOwnerSpellAndTrapCardsInZone(Player player, List<CardModel> into)
    {
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return;
        foreach (CardModel c in zone.Cards)
        {
            if (c is BaseSpellCard or BaseTrapCard)
                into.Add(c);
        }
    }
}
