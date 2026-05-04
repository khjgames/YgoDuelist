using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// While on the field: each monster that shares its card id with at least one other field monster gains +{Mgc} ATK and +{Mgc} DEF (per Kotodama source on the field).
/// </summary>
public sealed class Kotodama : EffectMonsterCard
{
    public Kotodama()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 0,
            baseDef: 16,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Normal;
    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (Owner == null)
            return StatEffectTotal.None;

        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner);
        if (field.Count < 2)
            return StatEffectTotal.None;

        string entry = target.Id.Entry;
        int sameCardCount = 0;
        foreach (BaseMonsterCard? m in field)
        {
            if (m != null && m.Id.Entry == entry)
                sameCardCount++;
        }

        if (sameCardCount < 2)
            return StatEffectTotal.None;

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(mgc, mgc);
    }
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 7m;
    }
}
