using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell;

/// <summary>
/// Foolish Burial – send 1 monster from your draw or discard pile to the Graveyard,
/// then send this card to the Graveyard as well (instead of discard).
/// Mirrors the old Java effect using the new pile system.
/// </summary>
public sealed class Foolish_Burial : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => Enumerable.Empty<DynamicVar>();
    
    public Foolish_Burial()
        : base(1, CardRarity.Rare, TargetType.Self, DuelMonsterRace.SpellNormal)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Foolish_Burial),
    };

    protected override void OnUpgrade()
    {
        // Match base game (see Eidolon): upgrade energy cost, not Cost field.
        EnergyCost.UpgradeBy(-1);
    }

    // Match base game pattern (see Clash): gate playability via IsPlayable.
    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner != null &&
        GetMonsterCardsFromDeckAndDiscard(Owner).Any();

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
            return;

        await SelectAndSendMonsterToGraveyard(choiceContext, player);
    }

    private async Task SelectAndSendMonsterToGraveyard(PlayerChoiceContext choiceContext, Player player)
    {
        // Collect all monster cards from draw + discard.
        var allMonsters = GetMonsterCardsFromDeckAndDiscard(player).ToList();
        if (allMonsters.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(
            SelectionScreenPrompt,
            1,
            1);

        var selected = await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            allMonsters,
            player,
            prefs);

        var chosen = selected.FirstOrDefault();
        if (chosen != null)
        {
            await SendCardToGraveyard(choiceContext, player, chosen);
        }
    }

    private static IEnumerable<CardModel> GetMonsterCardsFromDeckAndDiscard(Player player)
    {
        // In combat, monsters live in the draw pile (PileType.Draw) and discard pile.
        var deck = PileType.Draw.GetPile(player);
        var discard = PileType.Discard.GetPile(player);

        IEnumerable<CardModel> FromPile(CardPile? pile) =>
            pile?.Cards.Where(c => c is BaseMonsterCard) ?? Enumerable.Empty<CardModel>();

        return FromPile(deck).Concat(FromPile(discard));
    }

    private static async Task SendCardToGraveyard(PlayerChoiceContext choiceContext, Player player, CardModel card)
    {
        var graveyardPile = GraveyardPile.CustomType.GetPile(player);
        if (graveyardPile == null)
            return;

        await CardPileCmd.Add(
            new[] { card },
            graveyardPile,
            CardPilePosition.Top,
            card,
            false);
    }
}
