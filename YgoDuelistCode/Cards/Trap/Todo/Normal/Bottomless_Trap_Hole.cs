using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Bottomless_Trap_Hole : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 15m),
            new DynamicVar("Mgc2", 22m)
        };

    public Bottomless_Trap_Hole()
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

        int incoming = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature);
        if ((decimal)incoming < DynamicVars["Mgc"].BaseValue)
            return;

        await CreatureCmd.Damage(choiceContext, target, DynamicVars["Mgc2"].BaseValue, ValueProp.Unpowered, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(-5m);
        DynamicVars["Mgc2"].UpgradeValueBy(11m);
    }
}
