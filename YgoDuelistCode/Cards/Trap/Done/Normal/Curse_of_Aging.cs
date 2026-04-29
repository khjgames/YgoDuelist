using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

/// <summary>
/// Destroy 1 card in hand (to Graveyard). All enemies gain <c>{Mgc}</c> Weak and Vulnerable. Upgrade: <c>{Mgc}</c> is 2.
/// </summary>
public sealed class Curse_of_Aging : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public Curse_of_Aging()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
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
        typeof(Curse_of_Aging),
    };

    
    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            var hand = Owner?.PlayerCombatState?.Hand;
            return hand != null && hand.Cards.Count > 0;
        }
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        await TryDestroyOneHandCardToGraveyard(choiceContext);

        decimal stacks = DynamicVars["Mgc"].BaseValue;

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState))
        {
            if (!enemy.IsAlive)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, stacks, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(enemy, stacks, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);

    private async Task TryDestroyOneHandCardToGraveyard(PlayerChoiceContext choiceContext)
    {
        if (Owner == null)
            return;

        var hand = YgoPlayerPiles.Hand(Owner);
        if (hand == null || hand.Cards.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        CardModel? card = await YgoHandCardSelection.TryChooseSingleHandCardAsync<CardModel>(
            choiceContext,
            Owner,
            prefs,
            choiceBegunOptions: PlayerChoiceOptions.CancelPlayCardActions);
        if (card == null)
            return;

        var grave = YgoPlayerPiles.Graveyard(Owner);
        if (grave == null)
            return;

        await CardPileCmd.Add(new[] { card }, grave, CardPilePosition.Top, card, false);
    }
}
