using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Jirai_Gumo : EffectMonsterCard
{
    public Jirai_Gumo()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth | YgoCardPackTags.Insect | YgoCardPackTags.Burn | YgoCardPackTags.Chance;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    public override Type[] BundledCards => new[]
    {
        typeof(Jirai_Gumo),
        typeof(Second_Coin_Toss)
    };

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Jirai_Gumo),
        typeof(Second_Coin_Toss)
    };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
        DynamicVars["Def"].UpgradeValueBy(4m);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(4m);
    }
}
