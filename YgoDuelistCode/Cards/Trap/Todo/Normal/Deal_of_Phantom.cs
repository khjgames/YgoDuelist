using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Deal_of_Phantom : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public Deal_of_Phantom()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        int monstersInGrave = GraveyardRelic.GetGraveyardCards(Owner).Count(c => c is BaseMonsterCard);
        decimal per = DynamicVars["Mgc"].BaseValue;
        decimal block = per * monstersInGrave;
        if (block <= 0m)
            return;

        await CreatureCmd.GainBlock(Owner.Creature, block, default, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);
}
