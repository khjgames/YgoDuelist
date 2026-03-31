using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Dark_Master_Zorc : RitualMonsterCard
{
    public Dark_Master_Zorc()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 27,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] BundledCards => new[] { typeof(Contract_with_the_Dark_Master), typeof(Dark_Master_Zorc) };

    public override Type[] RelatedCards =>
        RitualArchetypeMeta.RelatedCardsForPairedRitual(typeof(Contract_with_the_Dark_Master), typeof(Dark_Master_Zorc));
}
