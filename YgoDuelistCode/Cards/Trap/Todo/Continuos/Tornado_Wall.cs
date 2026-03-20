using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Tornado_Wall : BaseTrapCard
{
    public Tornado_Wall()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapContinuous)
    {
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ExecuteTrapEffectPlaceholder(choiceContext, cardPlay);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        ExecuteTrapUpgradePlaceholder();
    }

    private void ExecuteTrapEffectPlaceholder(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
    }

    private void ExecuteTrapUpgradePlaceholder()
    {
    }
}
