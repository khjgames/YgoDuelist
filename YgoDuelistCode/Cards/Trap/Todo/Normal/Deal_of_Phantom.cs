using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Deal_of_Phantom : BaseTrapCard
{
    public Deal_of_Phantom()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner)?.OfType<BaseMonsterCard>() ?? Enumerable.Empty<BaseMonsterCard>();
        if (!field.Any())
            return;

        await CreatureCmd.GainBlock(Owner.Creature, 18m, default, cardPlay);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
