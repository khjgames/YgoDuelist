using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Dark_Mirror_Force : BaseTrapCard
{
    public Dark_Mirror_Force()
        : base(cost: 1, cardType: CardType.Attack, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Trap;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Dark_Mirror_Force),
    };

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || Owner?.Creature?.CombatState is not CombatState cs)
                return false;
            Creature pc = Owner.Creature;
            bool hasAttackingEnemy = cs.HittableEnemies.Any(e =>
                e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) > 0);
            bool hasNonAttackingEnemy = cs.HittableEnemies.Any(e =>
                e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) <= 0);
            return hasAttackingEnemy && hasNonAttackingEnemy;
        }
    }

    public override bool RefineIsValidTarget(Creature? target, bool vanillaResult)
    {
        if (!vanillaResult || target == null || Owner?.Creature == null)
            return vanillaResult;
        return YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature) > 0;
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

        foreach (Creature e in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState))
        {
            if (YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, Owner.Creature) > 0)
                continue;
            await CreatureCmd.Damage(choiceContext, e, damage, ValueProp.Unpowered, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
