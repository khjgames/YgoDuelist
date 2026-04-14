using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>If you control no other Insect-Type monsters, printed ATK/DEF become 10 (see cards.json).</summary>
public sealed class Arsenal_Bug : EffectMonsterCard
{
    public Arsenal_Bug()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 20,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Insect;

    public override Type[] RelatedCards => new[] { typeof(Arsenal_Bug) };

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null || HasOtherInsectOnField(Owner))
            return base.GetSecondaryStats();

        GetDynamicPrintedAtkDef(out int patk, out int pdef);
        return (10 - patk, 10 - pdef);
    }

    private bool HasOtherInsectOnField(Player player)
    {
        foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (ReferenceEquals(m, this))
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Insect)
                return true;
        }

        return false;
    }
}
