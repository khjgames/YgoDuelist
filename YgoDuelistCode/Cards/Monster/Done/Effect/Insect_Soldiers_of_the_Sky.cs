using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While you control another face-up Wind monster, gains printed <c>Mgc</c> ATK.</summary>
public sealed class Insect_Soldiers_of_the_Sky : EffectMonsterCard
{
    public override int AttackPortionCount => 3;
    public Insect_Soldiers_of_the_Sky()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 10,
            baseDef: 8,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Insect;

    public override Type[] RelatedCards => new[] { typeof(Insect_Soldiers_of_the_Sky) };

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        bool otherWind = DuelMonsterFieldRegistry
            .GetFieldMonsters(Owner)
            .Any(m => m != null && !m.FaceDown && !ReferenceEquals(m, this) && m.DuelMonsterAttribute == DuelMonsterAttribute.Wind);
        if (!otherWind)
            return (0, 0);

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (mgc, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 8m;
    }
}
