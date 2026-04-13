using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Fortress_Whale : RitualMonsterCard
{
    public Fortress_Whale()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 23,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fish)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Water | YgoCardPackTags.Ocean;

    public override Type[] BundledCards => new[] { typeof(Fortress_Whale_S_Oath), typeof(Fortress_Whale) };
}
