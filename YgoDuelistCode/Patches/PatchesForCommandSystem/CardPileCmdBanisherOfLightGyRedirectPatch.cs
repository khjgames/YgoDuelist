using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="YgoBanisherOfLightRules"/>: while a face-up Banisher of the Light is on the field, <see cref="CardPileCmd.Add"/> targets to the Graveyard become banish moves instead.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdBanisherOfLightGyRedirectPatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        IEnumerable<CardModel> cards,
        CardPile newPile,
        CardPilePosition position,
        AbstractModel source,
        bool allowNothing,
        ref Task __result)
    {
        if (newPile?.Type != GraveyardPile.CustomType)
            return true;

        List<CardModel> list = YgoMpCombatOrder.CardsSnapshotOrderedForMp(cards);
        if (list.Count == 0)
            return true;

        if (list[0].Owner is not Player p0 || p0.Creature?.CombatState is not CombatState cs)
            return true;

        if (!YgoBanisherOfLightRules.IsBanisherRedirectActive(cs))
            return true;

        foreach (CardModel c in list)
        {
            if (c.Owner is not Player pl || pl.Creature?.CombatState != cs)
                return true;
        }

        __result = BanishBatchInsteadOfGraveyardAsync(list, position, source, allowNothing);
        return false;
    }

    private static async Task BanishBatchInsteadOfGraveyardAsync(
        List<CardModel> list,
        CardPilePosition position,
        AbstractModel source,
        bool allowNothing)
    {
        _ = position;
        _ = source;
        _ = allowNothing;

        foreach (CardModel c in list)
        {
            if (c.Owner is Player pl)
                await YgoBanishedService.BanishCard(pl, c);
        }
    }
}
