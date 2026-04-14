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
        YgoCockroachKnightGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoMotherGrizzlyGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoSkullInvitationGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoBlackPendantGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoPinchHopperGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoFlyingKamakiri1Graveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoSkullMarkLadybugGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoOutstandingDogMarronGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoPyramidTurtleGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoMysticTomatoGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoShiningAngelGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoGiantGermGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoLordPoisonGraveyard.OnCardAddedToGraveyardPile(__instance, card);
        YgoManticoreOfDarknessEndPhase.OnCardAddedToGraveyardPile(__instance, card);
    }
}
