using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Cursed_Seal_of_the_Forbidden_Spell : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 3m) };

    public Cursed_Seal_of_the_Forbidden_Spell()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapCounter)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Cursed_Seal_of_the_Forbidden_Spell),
    };

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            var hand = Owner?.PlayerCombatState?.Hand;
            if (hand == null)
                return false;

            return hand.Cards.Any(c => c is IYgoCard y && y.YgoCardType == YgoCardType.Spell);
        }
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        CardModel? spell = await ChooseSpellToDiscard(choiceContext);
        if (spell == null)
            return;

        await CardCmd.Discard(choiceContext, spell);
        
        await PowerCmd.Apply<ArtifactPower>(Owner.Creature, 1m, Owner.Creature, this);
        await PowerCmd.Apply<PlatingPower>(Owner.Creature, DynamicVars["Mgc"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(2m);

    private async Task<CardModel?> ChooseSpellToDiscard(PlayerChoiceContext choiceContext)
    {
        if (Owner == null)
            return null;

        var hand = YgoPlayerPiles.Hand(Owner);
        if (hand == null || hand.Cards.Count == 0)
            return null;

        if (!hand.Cards.Any(c => c is IYgoCard y && y.YgoCardType == YgoCardType.Spell))
            return null;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        return await YgoHandCardSelection.TryChooseSingleHandCardAsync<CardModel>(
            choiceContext,
            Owner,
            prefs,
            predicate: c => c is IYgoCard y && y.YgoCardType == YgoCardType.Spell,
            choiceBegunOptions: PlayerChoiceOptions.CancelPlayCardActions);
    }
}
