using YgoDuelist.YgoDuelistCode.Cards;
using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Mask_of_Darkness : EffectMonsterCard
{
    public Mask_of_Darkness()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 9,
            baseDef: 4,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Mask_of_Darkness),
    };

    /// <summary>Upgrade (with inherited printed stat scaling): −1 energy for defense stance / Command Defend.</summary>
    public override int GetDuelMonsterDefensePlayEnergyDiscount() =>
        base.GetDuelMonsterDefensePlayEnergyDiscount() +
        (IsUpgradedOrPreviewActive ? 1 : 0);

}
