using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Gyaku_Gire_Panda : EffectMonsterCard
{
    public Gyaku_Gire_Panda()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 8,
            baseDef: 16,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Earth;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Gyaku_Gire_Panda),
    };

    /// <summary>+printed <c>Mgc</c> ATK per living hittable enemy (YGO: +300 per opponent monster).</summary>
    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner?.Creature?.CombatState == null)
            return (0, 0);

        int perEnemy = DynamicVars != null && DynamicVars.ContainsKey("Mgc")
            ? (int)DynamicVars["Mgc"].BaseValue
            : BaseMgc;

        int n = Owner.Creature.CombatState.HittableEnemies.Count(e => e.IsAlive);
        return (perEnemy * n, 0);
    }
}
