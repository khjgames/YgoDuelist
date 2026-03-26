using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

/// <summary>Trap: enemies that intend to attack you lose effective damage via Weak.</summary>
public sealed class Amazoness_Archers : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 2m) };

    public Amazoness_Archers()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies.Where(c => c.IsAlive))
        {
            if (YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, Owner.Creature) <= 0)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, DynamicVars["Mgc"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
