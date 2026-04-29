using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>1-cost; when this card executes a monster, permanently lose 2 ATK (1 if upgraded).</summary>
public sealed class Zombyra_the_Dark : EffectMonsterCard
{
    public Zombyra_the_Dark()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 21,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            duelMonsterAttackPlayEnergyOverride: 1)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Zombyra_the_Dark),
    };

    public override int PermanentAtkDeltaOnEnemyExecute => IsUpgraded ? -1 : -2;

    public override bool AppliesPermanentAtkDeltaOnEnemyKill(Creature killedEnemy) => killedEnemy.IsMonster;

    protected override void OnUpgrade() => base.OnUpgrade();
}
