using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When an equip spell leaves the Spell/Trap zone for the Graveyard, unregister its stat link.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdEquipSpellZoneDetachPatch
{
    [HarmonyPrefix]
    public static void Prefix(IEnumerable<CardModel> cards, CardPile newPile, ref List<(CardModel Card, PileType? From)>? __state)
    {
        __state = new List<(CardModel, PileType?)>();
        foreach (CardModel c in cards)
            __state.Add((c, c.Pile?.Type));
    }

    [HarmonyPostfix]
    public static void Postfix(CardPile newPile, List<(CardModel Card, PileType? From)>? __state)
    {
        if (__state == null)
            return;

        bool toGraveyard = newPile.Type == GraveyardPile.CustomType;
        bool toLimbo = newPile.Type == LimboPile.CustomType;

        foreach ((CardModel card, PileType? from) in __state)
        {
            if (from == GraveyardPile.CustomType && newPile.Type != GraveyardPile.CustomType)
            {
                if (card is IYgoSpellTrapEquipLink)
                    YgoSpellTrapEquipLinkRegistry.Detach(card);
            }

            if (from != SpellTrapZonePile.CustomType)
                continue;

            if (card is BaseEquipSpellCard eq)
            {
                bool leaveZoneToLimboUnion = toLimbo && eq is IYgoUnionEquipSpell;
                if (!toGraveyard && !leaveZoneToLimboUnion)
                    continue;

                YgoEquipSpellRegistry.Detach(eq);
                if (eq.Owner != null)
                {
                    YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(eq.Owner);
                    YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(eq.Owner);
                }

                if (toGraveyard
                    && eq is IYgoUnionEquipSpell
                    && eq.Owner is Player unionPl
                    && YgoUnionLimboRegistry.TryGetByEquip(eq, out BaseMonsterCard? limboMon, out _, out _)
                    && limboMon != null
                    && limboMon.Pile?.Type == LimboPile.CustomType)
                {
                    TaskHelper.RunSafely(YgoUnionLimboDestroyed.AfterEquipArrivedInGraveyardFromZoneAsync(unionPl, eq, limboMon));
                }

                if (toGraveyard && eq.Owner is Player gyOwner && eq is IYgoEquipSentFromSpellTrapZoneToGraveyard gyHook)
                {
                    TaskHelper.RunSafely(gyHook.OnSentFromSpellTrapZoneToGraveyardAsync(gyOwner));
                }

                continue;
            }

            if (card is IYgoSpellTrapEquipLink linkTrap)
            {
                if (toGraveyard)
                {
                    if (!linkTrap.DetachSpellTrapEquipLinkOnSpellTrapZoneToGraveyard)
                        continue;

                    BaseMonsterCard? linked = YgoSpellTrapEquipLinkRegistry.DetachAndConsumeLinkedMonster(card);
                    if (linked != null && card.Owner != null && linkTrap.DestroyLinkedDuelMonsterOnSpellTrapZoneToGraveyard)
                        TaskHelper.RunSafely(YgoSpellTrapEquipLinkCombat.DestroyLinkedMonsterIfOnFieldAsync(card.Owner, linked));
                }
                else if (newPile.Type != SpellTrapZonePile.CustomType)
                    YgoSpellTrapEquipLinkRegistry.Detach(card);
            }

            if (from == SpellTrapZonePile.CustomType && newPile.Type != SpellTrapZonePile.CustomType
                && card.Owner != null
                && YgoSarcophagusChain.IsSarcophagusPiece(card))
            {
                Player owner = card.Owner;
                TaskHelper.RunSafely(
                    YgoSarcophagusChain.OnSarcophagusPieceRemovedFromFieldAsync(
                        YgoChoiceContexts.Blocking(),
                        owner,
                        card));
            }

            if (from == SpellTrapZonePile.CustomType && newPile.Type != SpellTrapZonePile.CustomType && card.Owner != null)
                TaskHelper.RunSafely(DragonRageService.SyncPlayerPowerAsync(card.Owner));
        }
    }
}
