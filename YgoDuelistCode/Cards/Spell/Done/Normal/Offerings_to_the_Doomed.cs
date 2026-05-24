using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

/// <summary>
/// Reveal the top 3 (5 if upgraded) cards of your deck; send 2 to the Graveyard and the rest to the bottom.
/// Gain 1 Energy and 1 Conduit.
/// </summary>
public sealed class Offerings_to_the_Doomed : BaseSpellCard
{
    private const int GraveyardSendCount = 2;
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    private static readonly LocString RevealPreviewPrompt =
        new("cards", "YGODUELIST-OFFERINGS_TO_THE_DOOMED.reveal_preview");

    private static readonly LocString GraveyardSelectPrompt =
        new("cards", "YGODUELIST-OFFERINGS_TO_THE_DOOMED.graveyard_selection");

    public override bool UseAlternateUpgradedDescription => true;

    public Offerings_to_the_Doomed()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Dark;

    public override Type[] RelatedCards => new[]
    {
        typeof(Offerings_to_the_Doomed),
        typeof(Tribute_to_the_Doomed),
        typeof(Spellbook_Organization),
    };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && CountDrawPileCards(Owner) > 0;

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player?.Creature == null)
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
        if (draw == null || graveyard == null)
            return;

        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        int revealCount = IsUpgraded ? 5 : 3;
        List<CardModel> revealed = TakeTopCardsFromDraw(draw, revealCount);
        if (revealed.Count == 0)
            return;

        await YgoPreviewGridSelection.ShowPreviewAsync(
            choiceContext,
            revealed,
            player,
            RevealPreviewPrompt);

        int toGraveyardCount = Math.Min(GraveyardSendCount, revealed.Count);
        List<CardModel> toGraveyard;
        if (toGraveyardCount == revealed.Count)
        {
            toGraveyard = revealed;
        }
        else
        {
            var prefs = new CardSelectorPrefs(GraveyardSelectPrompt, toGraveyardCount, toGraveyardCount)
            {
                RequireManualConfirmation = true,
                Cancelable = false,
            };

            toGraveyard = (await YgoOrderedCardSelection.TryChooseManyAsync(
                choiceContext,
                player,
                prefs,
                () => revealed,
                toGraveyardCount,
                PlayerChoiceOptions.CancelPlayCardActions)).Cast<CardModel>().ToList();

            if (toGraveyard.Count != toGraveyardCount)
                return;
        }

        foreach (CardModel card in toGraveyard)
            await CardPileCmd.Add(card, graveyard, CardPilePosition.Top, this, false);

        List<CardModel> toBottom = revealed.Where(c => !toGraveyard.Contains(c)).ToList();
        for (int i = toBottom.Count - 1; i >= 0; i--)
            await CardPileCmd.Add(toBottom[i], draw, CardPilePosition.Bottom, this, false);

        await PlayerCmd.GainEnergy(1, player);
        await PlayerCmd.GainStars(1, player);
    }

    private static int CountDrawPileCards(Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        return draw?.Cards.Count ?? 0;
    }

    private static List<CardModel> TakeTopCardsFromDraw(CardPile draw, int count)
    {
        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards).Take(count).ToList();
    }
}
