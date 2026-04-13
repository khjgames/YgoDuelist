using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Zera_the_Mant : RitualMonsterCard
{
    public Zera_the_Mant()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 28,
            baseDef: 23,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] BundledCards => new[] { typeof(Zera_Ritual), typeof(Zera_the_Mant) };
}
