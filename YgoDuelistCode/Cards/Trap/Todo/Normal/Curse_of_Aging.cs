using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Curse_of_Aging : BaseTrapCard
{
    public Curse_of_Aging()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

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

        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            if (!enemy.IsAlive)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, 1m, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(enemy, 1m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }

    private async Task TryDestroyOneHandCardToGraveyard(PlayerChoiceContext choiceContext)
    {
        if (Owner == null)
            return;

        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null || hand.Cards.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            _ => true,
            this);

        var card = selected.FirstOrDefault();
        if (card == null)
            return;

        var grave = GraveyardPile.CustomType.GetPile(Owner);
        if (grave == null)
            return;

        await CardPileCmd.Add(new[] { card }, grave, CardPilePosition.Top, card, false);
    }
}
