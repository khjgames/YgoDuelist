using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Power;

public sealed class Fallen_Insights : BaseYgoPowerCard
{
    public override bool UseAlternateUpgradedDescription => true;

    public Fallen_Insights()
        : base(cost: 2, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    protected override async Task OnPowerPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        await PowerCmd.Apply<FallenInsightsPower>(Owner.Creature, 1m, Owner.Creature, this);
    }
}
