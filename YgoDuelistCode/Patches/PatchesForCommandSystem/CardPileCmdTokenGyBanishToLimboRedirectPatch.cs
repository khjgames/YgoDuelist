using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// YGO Tokens never enter the Graveyard or Banished piles — any move targeting those zones goes to Limbo instead
/// (field removal, destruction, banish effects, etc.).
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdTokenGyBanishToLimboRedirectPatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        IEnumerable<CardModel> cards,
        CardPile newPile,
        CardPilePosition position,
        AbstractModel source,
        bool skipVisuals,
        ref Task __result)
    {
        if (newPile == null)
            return true;
        if (newPile.Type != GraveyardPile.CustomType && newPile.Type != BanishedPile.CustomType)
            return true;

        List<CardModel> list = YgoMpCombatOrder.CardsSnapshotOrderedForMp(cards);
        if (list.Count == 0)
            return true;

        List<CardModel> tokens = list.Where(c => c is IYgoTokenMonster).ToList();
        if (tokens.Count == 0)
            return true;

        List<CardModel> nonTokens = tokens.Count == list.Count
            ? []
            : list.Where(c => c is not IYgoTokenMonster).ToList();

        __result = RedirectTokensToLimboAsync(tokens, nonTokens, newPile, position, source, skipVisuals);
        return false;
    }

    private static async Task RedirectTokensToLimboAsync(
        List<CardModel> tokens,
        List<CardModel> nonTokens,
        CardPile originalDestination,
        CardPilePosition position,
        AbstractModel source,
        bool skipVisuals)
    {
        foreach (CardModel token in tokens)
        {
            if (token.Owner is Player owner)
                await YgoLimboService.SendToLimboAsync(owner, token);
        }

        if (nonTokens.Count > 0)
        {
            await CardPileCmd.Add(
                nonTokens,
                originalDestination,
                position,
                source,
                skipVisuals);
        }
    }
}
