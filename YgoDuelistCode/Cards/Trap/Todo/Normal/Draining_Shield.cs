using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
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

public sealed class Draining_Shield : BaseTrapCard
{
    public Draining_Shield()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Heal | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[] { typeof(Draining_Shield) };

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || Owner?.Creature?.CombatState == null)
                return false;
            return AnyEnemyWithAttackIntent(Owner);
        }
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive || target.Side != CombatSide.Enemy)
            return;

        int atk = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature);
        if (atk <= 0)
            return;

        await CreatureCmd.Heal(Owner.Creature, atk);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private static bool AnyEnemyWithAttackIntent(Player player)
    {
        Creature? pc = player.Creature;
        if (pc?.CombatState is not CombatState cs)
            return false;
        return cs.HittableEnemies.Any(e => e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) > 0);
    }
}
