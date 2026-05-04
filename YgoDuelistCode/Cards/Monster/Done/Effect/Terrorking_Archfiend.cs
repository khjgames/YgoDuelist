using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Terrorking_Archfiend : EffectMonsterCard
{
    public Terrorking_Archfiend()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 3)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Terrorking_Archfiend),
    };

    public override int PermanentAtkDeltaOnEnemyExecute => IsUpgraded ? 3 : 2;

    public override int GetDuelMonsterPlayEnergyDiscount() =>
        (IsUpgraded && IsAttackBattlePosition ? 1 : 0) + GetCostDownHandPlayEnergyDiscount();

    /// <summary>Upgrade reduces attack play energy; Command Attack must show that cost even when the monster is in DEF on field.</summary>
    public override int GetDuelMonsterPlayEnergyDiscountForFieldCommandAttack() =>
        IsUpgraded ? 1 : 0;

    /// <summary>Attack-position upgrade does not apply to Command Defend.</summary>
    public override int GetDuelMonsterPlayEnergyDiscountForFieldCommandDefend() => 0;

    protected override void OnUpgrade() => base.OnUpgrade();
}
