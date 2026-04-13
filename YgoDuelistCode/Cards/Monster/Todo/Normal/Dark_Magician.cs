using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;

public sealed class Dark_Magician : NormalMonsterCard
{
    public Dark_Magician()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 25,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Dark |
        YgoCardPackTags.Spellcaster |
        YgoCardPackTags.Normal;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};


    public override Type[] RelatedCards => GetRelatedCards();
}