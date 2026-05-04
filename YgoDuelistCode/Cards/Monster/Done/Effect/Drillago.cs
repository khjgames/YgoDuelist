using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Drillago : EffectMonsterCard
{
    public override int AttackPortionCount => 4;
    public Drillago()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 16,
            baseDef: 11,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Machine | YgoCardPackTags.Dark | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Drillago) };

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

    public override bool AttackDealsFullBlightedDamage => TargetIntendsToAttack();

    private bool TargetIntendsToAttack()
    {
        if (Owner?.Creature?.CombatState == null)
            return false;
        foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(Owner.Creature.CombatState.HittableEnemies))
        {
            if (!enemy.IsAlive)
                continue;
            if (YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, Owner.Creature) > 0)
                return true;
        }
        return false;
    }

}
