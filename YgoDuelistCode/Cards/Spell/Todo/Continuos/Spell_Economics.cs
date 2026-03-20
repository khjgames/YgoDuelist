using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Spell_Economics : BaseSpellCard
{
    public Spell_Economics()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellContinuous)
    {
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ExecuteSpellEffectPlaceholder(choiceContext, cardPlay);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        ExecuteSpellUpgradePlaceholder();
    }

    private void ExecuteSpellEffectPlaceholder(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
    }

    private void ExecuteSpellUpgradePlaceholder()
    {
    }
}
