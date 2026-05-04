using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;

public sealed class X_Head_Cannon : NormalMonsterCard
{
    public override int AttackPortionCount => 2;
    public X_Head_Cannon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 18,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Light |
        YgoCardPackTags.Machine |
        YgoCardPackTags.Normal | YgoCardPackTags.Bundled;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    public override Type[] BundledCards => new[]
    {
        typeof(Y_Dragon_Head), typeof(Z_Metal_Tank)
    };


    // You will see these related cards more often with this card in your deck or side deck.
    //public override Type[] RelatedCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

}