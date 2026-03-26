using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Bowganian : EffectMonsterCard
{
    public Bowganian()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 13,
            baseDef: 10,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (!YgoAnnualTracker.TryConsumeAnnual(Owner, "BOWGANIAN"))
            return;

        var target = cardPlay.Target;
        if (target == null)
        {
            var cs = Owner.Creature.CombatState;
            var list = YgoDeterministicRng
                .StableOrder(cs.HittableEnemies.Where(c => c.IsAlive), c => c.CombatId)
                .ToList();
            if (list.Count == 0)
                return;
            target = YgoDeterministicRng.PickOne(cs, list, "BOWGANIAN-RANDOM_TARGET");
        }

        if (target != null)
        {
            await DamageCmd.Attack(DynamicVars["Mgc"].BaseValue)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 9m;
    }
}
