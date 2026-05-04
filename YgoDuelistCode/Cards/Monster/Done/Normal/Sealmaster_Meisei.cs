using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;

public sealed class Sealmaster_Meisei : NormalMonsterCard, IYgoSealmasterMeiseiFieldMonster
{
    public Sealmaster_Meisei()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 11,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Dark |
        YgoCardPackTags.Spellcaster | YgoCardPackTags.Normal | YgoCardPackTags.Spell | YgoCardPackTags.Trap | YgoCardPackTags.Bundled;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    public override Type[] BundledCards => new[]
    {
        typeof(Talisman_of_Spell_Sealing),
        typeof(Talisman_of_Trap_Sealing),
    };


    // You will see these related cards more often with this card in your deck or side deck.
    //public override Type[] RelatedCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

}