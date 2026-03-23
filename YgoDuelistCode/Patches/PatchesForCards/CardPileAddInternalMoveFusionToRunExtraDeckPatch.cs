using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Any fusion monster that would sit on the master <see cref="Player.Deck"/> is moved immediately to the run Extra Deck pile.
/// </summary>
[HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
public static class CardPileAddInternalMoveFusionToRunExtraDeckPatch
{
    public static void Postfix(CardPile __instance, CardModel card)
    {
        if (card is not FusionMonsterCard)
            return;

        Player? owner = card.Owner;
        if (owner == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(owner))
            return;

        if (!ReferenceEquals(__instance, owner.Deck))
            return;

        CardPile runExtra = PlayerRunExtraDeck.GetOrCreatePile(owner);
        if (ReferenceEquals(__instance, runExtra))
            return;

        __instance.RemoveInternal(card, silent: true);
        runExtra.AddInternal(card, -1, silent: true);
        ExtraDeckRelic.NotifyRunExtraDeckChanged(owner);
    }
}
