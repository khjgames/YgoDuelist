using YgoDuelist.YgoDuelistCode.Cards;
using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Chaos_Necromancer : EffectMonsterCard
{
    public Chaos_Necromancer()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 1)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Draw | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Chaos_Necromancer),
    };

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int gyMonsters = YgoPlayerPiles.GraveyardCards(Owner).Count(c => c is BaseMonsterCard);
        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (gyMonsters * mgc, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
    }
}
