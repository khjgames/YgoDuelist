using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Black_Luster_Soldier : RitualMonsterCard
{
    public Black_Luster_Soldier()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 30,
            baseDef: 25,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Ritual | YgoCardPackTags.Earth | YgoCardPackTags.Warrior;

    public override Type[] BundledCards => new[] { typeof(Black_Luster_Ritual), typeof(Black_Luster_Soldier) };
}
