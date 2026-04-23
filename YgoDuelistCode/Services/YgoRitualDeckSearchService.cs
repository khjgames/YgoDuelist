using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public enum RitualDeckSearchKind
{
    RitualMonsterOnly,
    RitualSpellOnly,
    RitualMonsterOrSpell
}

/// <summary>Normal/Flip Summon search: add 1 Ritual Monster and/or Ritual Spell from the deck to the hand.</summary>
public static class YgoRitualDeckSearchService
{
    private static readonly LocString SelectPrompt = new("cards", "YGODUELIST-RITUAL_DECK_SEARCH.select_prompt");

    public static async Task TrySearchAndAddToHandAsync(
        Player player,
        PlayerChoiceContext choiceContext,
        RitualDeckSearchKind kind)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (draw == null || hand == null)
            return;

        var ctx = YgoChoiceContexts.Blocking(choiceContext);
        var prefs = new CardSelectorPrefs(SelectPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<CardModel> BuildCandidates() => BuildDeckCandidates(draw, kind);

        CardModel? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync<CardModel>(
            ctx,
            player,
            prefs,
            BuildCandidates);
        if (chosen == null)
            return;

        await CardPileCmd.Add(new CardModel[] { chosen }, hand, CardPilePosition.Top, chosen, false);
    }

    private static List<CardModel> BuildDeckCandidates(CardPile draw, RitualDeckSearchKind kind)
    {
        var list = new List<CardModel>();
        foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards))
        {
            bool isRm = c is RitualMonsterCard;
            bool isRs = c is RitualSpellCard;
            switch (kind)
            {
                case RitualDeckSearchKind.RitualMonsterOnly:
                    if (isRm)
                        list.Add(c);
                    break;
                case RitualDeckSearchKind.RitualSpellOnly:
                    if (isRs)
                        list.Add(c);
                    break;
                case RitualDeckSearchKind.RitualMonsterOrSpell:
                    if (isRm || isRs)
                        list.Add(c);
                    break;
            }
        }

        return list;
    }
}
