using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Black_Tyranno : EffectMonsterCard
{
    public override int AttackPortionCount => 2;
    public Black_Tyranno()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 26,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dinosaur)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Earth | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Black_Tyranno) };

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

    public override bool AttackDealsFullBlightedDamage => TargetHasBlockOrPlansToBlock();

    private bool TargetHasBlockOrPlansToBlock()
    {
        if (Owner?.Creature?.CombatState == null)
            return false;
        foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(Owner.Creature.CombatState.HittableEnemies))
        {
            if (!enemy.IsAlive)
                continue;
            if (enemy.Block > 0)
                return true;
        }
        return false;
    }

}
