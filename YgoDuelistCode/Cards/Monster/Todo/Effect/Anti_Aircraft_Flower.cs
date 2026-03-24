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

public sealed class Anti_Aircraft_Flower : EffectMonsterCard
{
    public Anti_Aircraft_Flower()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 0,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var list = Owner.Creature.CombatState.HittableEnemies.Where(c => c.IsAlive).ToList();
        if (list.Count == 0)
            return;

        var target = Owner.RunState.Rng.CombatTargets.NextItem(list);
        if (target == null)
            return;

        await PowerCmd.Apply<VulnerablePower>(target, 2m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
