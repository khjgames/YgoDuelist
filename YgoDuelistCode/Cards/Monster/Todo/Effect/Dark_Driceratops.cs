using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Dark_Driceratops : EffectMonsterCard
{
    public Dark_Driceratops()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 24,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dinosaur)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Dark_Driceratops) };

    public override bool CardShowsSplinterKeyword => true;

    public override bool AttackDealsSplinterDamage => true;

}
