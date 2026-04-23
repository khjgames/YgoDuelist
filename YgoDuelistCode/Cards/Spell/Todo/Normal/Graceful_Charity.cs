using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>Draw 3 cards, then destroy 2 cards in your hand (sent to Graveyard).</summary>
public sealed class Graceful_Charity : BaseSpellCard
{
    private static readonly LocString HandDestroyPrompt =
        new LocString("cards", "YGODUELIST-GRACEFUL_CHARITY.hand_destroy_selection");

    public Graceful_Charity()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[]
    {
        typeof(Graceful_Charity),
        typeof(Enraged_Muka_Muka),
        typeof(Pot_Of_Greed),
        typeof(Upstart_Goblin),
        typeof(Thunder_Dragon),
        typeof(Spellbook_Organization),
        typeof(Rush_Recklessly),
    };

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        await CardPileCmd.Draw(choiceContext, 3, Owner);

        var hand = YgoPlayerPiles.Hand(Owner);
        if (hand == null)
            return;

        int selectable = hand.Cards.Count(c => !ReferenceEquals(c, this));
        int toDestroy = Math.Min(2, selectable);
        if (toDestroy == 0)
            return;
        var prefs = new CardSelectorPrefs(HandDestroyPrompt, toDestroy, toDestroy)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        List<CardModel> cards = (await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            Owner,
            prefs,
            () => BuildHandDestroyCandidates(Owner, this),
            toDestroy,
            PlayerChoiceOptions.CancelPlayCardActions)).Cast<CardModel>().ToList();
        if (cards.Count == 0)
            return;

        var grave = YgoPlayerPiles.Graveyard(Owner);
        if (grave == null)
            return;

        await CardPileCmd.Add(cards, grave, CardPilePosition.Top, cards[0], false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static List<CardModel> BuildHandDestroyCandidates(Player player, CardModel sourceCard) =>
        TributeSummonGridSelect.BuildStabilizedHandCandidates(player, null, sourceCard);
}
