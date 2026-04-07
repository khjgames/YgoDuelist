using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Duel monsters use <see cref="CardType.Skill"/> while in defense position, but vanilla enchantments (e.g. Corrupted) only
/// allow <see cref="CardType.Attack"/>. Re-evaluate using <see cref="AbstractMonsterCard.RegisteredCardType"/> when the
/// runtime type is Skill but the card was defined as an Attack monster.
/// </summary>
[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.CanEnchant))]
public static class EnchantmentModelCanEnchantYgoMonsterPatch
{
    [HarmonyPostfix]
    public static void Postfix(EnchantmentModel __instance, CardModel card, ref bool __result)
    {
        if (__result)
            return;
        if (card is not AbstractMonsterCard am || am.RegisteredCardType != CardType.Attack)
            return;
        if (card.Type != CardType.Skill)
            return;

        CardType runtimeType = card.Type;
        if ((uint)(runtimeType - 4) <= 2u)
            return;

        if (!__instance.CanEnchantCardType(CardType.Attack))
            return;

        CardPile? pile = card.Pile;
        if (pile != null && pile.Type == PileType.Deck && card.Keywords.Contains(CardKeyword.Unplayable))
            return;

        if (card.Enchantment != null && (!__instance.IsStackable || card.Enchantment.GetType() != __instance.GetType()))
            return;

        __result = true;
    }
}
