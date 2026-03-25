using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Dark_Mirror_Force : BaseTrapCard
{
    public Dark_Mirror_Force()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var attacker = cardPlay.Target;
        if (attacker == null || !attacker.IsAlive)
            return;

        int refDmg = YgoIntentAttackDamage.GetTotalAttackIntentDamage(attacker, Owner.Creature);
        if (refDmg <= 0)
            return;

        decimal damage = refDmg;

        foreach (Creature e in Owner.Creature.CombatState.HittableEnemies.Where(c => c.IsAlive))
        {
            if (YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, Owner.Creature) > 0)
                continue;
            await CreatureCmd.Damage(choiceContext, e, damage, ValueProp.Unpowered, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
