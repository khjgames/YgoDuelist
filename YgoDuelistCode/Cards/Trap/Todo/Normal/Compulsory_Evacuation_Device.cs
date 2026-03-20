using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Compulsory_Evacuation_Device : BaseTrapCard
{
    public Compulsory_Evacuation_Device()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
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
