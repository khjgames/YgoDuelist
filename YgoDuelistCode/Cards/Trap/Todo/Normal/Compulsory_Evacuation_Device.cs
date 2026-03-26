using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Compulsory_Evacuation_Device : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 5m) };

    public Compulsory_Evacuation_Device()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        await PowerCmd.Apply<YgoTemporaryStrengthLossPower>(target, DynamicVars["Mgc"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(2m);
}
