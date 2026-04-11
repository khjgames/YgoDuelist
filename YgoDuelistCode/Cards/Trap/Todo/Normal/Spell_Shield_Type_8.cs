using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Spell_Shield_Type_8 : BaseTrapCard
{
    private static readonly LocString SendSpellToGraveyardPrompt =
        new("cards", "YGODUELIST-SPELL_SHIELD_TYPE_8.spell_selection");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar("Mgc", 7m),
            new DynamicVar("Mgc2", 14m),
        };

    public Spell_Shield_Type_8()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapCounter)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Spell_Shield_Type_8),
    };

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || Owner.Creature == null)
            return;

        decimal block = DynamicVars["Mgc"].BaseValue;

        CardPile? graveyard = GraveyardPile.CustomType.GetPile(Owner);
        if (graveyard != null)
        {
            // Optional: send a Spell from hand to Graveyard; cancel = base block only.
            CardModel? chosenSpell = await TryChooseSpellToSendToGraveyard(choiceContext, Owner);
            if (chosenSpell != null)
            {
                await CardPileCmd.Add(new[] { chosenSpell }, graveyard, CardPilePosition.Top, this, false);
                block = DynamicVars["Mgc2"].BaseValue;
            }
        }

        await CreatureCmd.GainBlock(Owner.Creature, block, default, cardPlay);
    }

    private async Task<CardModel?> TryChooseSpellToSendToGraveyard(PlayerChoiceContext choiceContext, Player player)
    {
        var hand = PileType.Hand.GetPile(player);
        if (hand == null || hand.Cards.Count == 0)
            return null;

        bool anySpell = hand.Cards.Any(c => c is IYgoCard y && y.YgoCardType == YgoCardType.Spell);
        if (!anySpell)
            return null;

        var prefs = new CardSelectorPrefs(SendSpellToGraveyardPrompt, 0, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true,
        };

        List<CardModel> candidates = TributeSummonGridSelect.BuildStabilizedHandCandidates(
            player,
            c => c is IYgoCard y && y.YgoCardType == YgoCardType.Spell,
            null);

        var selected = await TributeSummonGridSelect.FromSimpleGridCombat(
            choiceContext,
            candidates,
            player,
            prefs,
            rebuildCanonicalForRemoteApply: () => TributeSummonGridSelect.BuildStabilizedHandCandidates(
                player,
                c => c is IYgoCard y && y.YgoCardType == YgoCardType.Spell,
                null),
            PlayerChoiceOptions.CancelPlayCardActions);

        return selected.FirstOrDefault();
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(3m);
        DynamicVars["Mgc2"].UpgradeValueBy(6m);
    }
}
