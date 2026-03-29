using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Poison_Mummy : EffectMonsterCard, IMonsterFlipEffect
{
    public Poison_Mummy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 10,
            baseDef: 18,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Poison_Mummy || Owner?.Creature?.CombatState == null)
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<Creature> enemies = YgoDeterministicRng
            .StableOrder(cs.HittableEnemies.Where(c => c.IsAlive), c => c.CombatId)
            .ToList();
        if (enemies.Count == 0)
            return;

        Creature? target = enemies.Count == 1
            ? enemies[0]
            : YgoDeterministicRng.PickOne(cs, enemies, "POISON_MUMMY-FLIP");

        if (target == null || !target.IsAlive)
            return;

        await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 8m;
    }
}
