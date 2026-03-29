using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>
/// Gains Mgc ATK for each face-up Plant monster on your field (including this card).
/// </summary>
public sealed class Wodan_the_Resident_of_the_Forest : EffectMonsterCard
{
    public Wodan_the_Resident_of_the_Forest()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 9,
            baseDef: 12,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int plantCount = 0;
        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner);
        foreach (BaseMonsterCard? m in field)
        {
            if (m == null || m.FaceDown)
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Plant)
                plantCount++;
        }

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (plantCount * mgc, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
