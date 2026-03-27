using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Fairy_Box : BaseContinuousTrapCard
{
    public Fairy_Box()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        // Heads/Tails + Weak is once per turn after you pay upkeep (see FairyBoxFieldPower), not on activation.
        await PowerCmd.Apply<FairyBoxFieldPower>(Owner.Creature, 1m, Owner.Creature, this);
    }
}
