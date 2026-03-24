using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Dark_Jeroid : EffectMonsterCard
{
    public Dark_Jeroid()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 12,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var cs = Owner.Creature.CombatState;
        var list = cs.HittableEnemies.Where(c => c.IsAlive).ToList();
        if (list.Count == 0)
            return;

        var target = Owner.RunState.Rng.CombatTargets.NextItem(list);
        if (target == null)
            return;

        await PowerCmd.Apply<StrengthPower>(target, -8m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
