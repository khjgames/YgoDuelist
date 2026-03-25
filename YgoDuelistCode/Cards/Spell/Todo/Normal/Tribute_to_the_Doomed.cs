using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>Discard 1 card, then destroy one enemy (heavy damage).</summary>
public sealed class Tribute_to_the_Doomed : BaseSpellCard
{
    public Tribute_to_the_Doomed()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && PileType.Hand.GetPile(Owner)?.Cards.Any(c => !ReferenceEquals(c, this)) == true;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || Owner.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        CardModel? toDiscard = await ChooseOtherHandCardToDiscard(choiceContext);
        if (toDiscard == null)
            return;

        await CardCmd.Discard(choiceContext, toDiscard);

        await CreatureCmd.Damage(choiceContext, target, 25m, ValueProp.Unpowered, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private async Task<CardModel?> ChooseOtherHandCardToDiscard(PlayerChoiceContext choiceContext)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner!,
            prefs,
            c => !ReferenceEquals(c, this),
            this);

        return selected.FirstOrDefault();
    }
}
