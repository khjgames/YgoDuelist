using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Power;

public sealed class Guardian_Spirit : BaseYgoPowerCard<GuardianSpiritPower>
{
    public override bool UseAlternateUpgradedDescription => true;

    public Guardian_Spirit()
        : base(cost: 2, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    protected override async Task OnPowerPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        decimal block = IsUpgraded ? 3m : 2m;
        await PowerCmd.Apply<GuardianSpiritPower>(Owner.Creature, block, Owner.Creature, this);
    }
}
