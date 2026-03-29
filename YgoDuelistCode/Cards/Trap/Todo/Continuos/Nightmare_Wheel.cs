using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Nightmare_Wheel : BaseContinuousTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar("Mgc", 5m),
        };

    public Nightmare_Wheel()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.AnyEnemy)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive || !target.CombatId.HasValue)
            return;

        if (IsUpgraded)
            await PowerCmd.Apply<NightmareWheelPlusPower>(target, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<NightmareWheelPower>(target, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(4m);
    }
}
