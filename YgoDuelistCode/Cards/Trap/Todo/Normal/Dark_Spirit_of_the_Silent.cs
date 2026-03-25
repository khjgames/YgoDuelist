using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Dark_Spirit_of_the_Silent : BaseTrapCard
{
    public Dark_Spirit_of_the_Silent()
        : base(cost: 2, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || Owner?.Creature?.CombatState == null)
                return false;
            return CountEnemiesWithAttackIntent(Owner) >= 2;
        }
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        var cs = Owner.Creature.CombatState;
        if (CountEnemiesWithAttackIntent(Owner) < 2)
            return Task.CompletedTask;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return Task.CompletedTask;
        if (YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature) <= 0)
            return Task.CompletedTask;

        YgoDarkSpiritSilentState.Activate(Owner, target, cs);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private static int CountEnemiesWithAttackIntent(Player player)
    {
        Creature? pc = player.Creature;
        CombatState? cs = pc?.CombatState;
        if (cs == null)
            return 0;
        return cs.HittableEnemies.Count(e => e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) > 0);
    }
}
