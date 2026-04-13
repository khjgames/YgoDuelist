using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Dokurorider : RitualMonsterCard
{
    public Dokurorider()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 19,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override Type[] BundledCards => new[] { typeof(Revival_of_Dokurorider), typeof(Dokurorider) };
}
