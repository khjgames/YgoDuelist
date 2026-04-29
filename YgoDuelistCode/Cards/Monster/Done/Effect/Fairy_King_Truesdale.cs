using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While in Defense Position, all Plant monsters you control gain 5 ATK and DEF (including this card).</summary>
public sealed class Fairy_King_Truesdale : EffectMonsterCard
{
    private const int FieldBonus = 5;

    public Fairy_King_Truesdale()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 22,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water | YgoCardPackTags.Ocean;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.GenericAllMonstersContinuousStatBoost;

    public override Type[] RelatedCards => new[] { typeof(Fairy_King_Truesdale) };

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (Owner == null || target.Owner != Owner)
            return StatEffectTotal.None;
        if (target.DuelMonsterRace != DuelMonsterRace.Plant)
            return StatEffectTotal.None;
        if (IsAttackBattlePosition)
            return StatEffectTotal.None;

        return new StatEffectTotal(FieldBonus, FieldBonus);
    }
}
