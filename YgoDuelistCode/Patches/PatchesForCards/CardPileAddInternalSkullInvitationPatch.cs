using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
public static class CardPileAddInternalSkullInvitationPatch
{
    static void Postfix(CardPile __instance, CardModel card, int index, bool silent)
    {
        YgoSkullInvitationGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoBlackPendantGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoPinchHopperGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoSkullMarkLadybugGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoOutstandingDogMarronGraveyard.OnCardAddedToGraveyardPile(__instance, card);
    }
}
