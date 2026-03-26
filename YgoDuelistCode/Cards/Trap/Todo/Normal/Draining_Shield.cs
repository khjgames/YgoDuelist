using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Draining_Shield : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public Draining_Shield()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        decimal healPer = DynamicVars["Mgc"].BaseValue;
        decimal healTotal = 0m;
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            if (!enemy.IsAlive)
                continue;
            int d = YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, Owner.Creature);
            if (d > 0)
                healTotal += healPer;
        }

        if (healTotal > 0m)
            await CreatureCmd.Heal(Owner.Creature, healTotal);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);
}
