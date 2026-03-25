using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class The_Legendary_Fisherman : EffectMonsterCard
{
    public The_Legendary_Fisherman()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 18,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override int GetDuelMonsterPlayEnergyDiscount()
    {
        if (IsCanonical || Owner == null)
            return 0;
        foreach (BaseFieldSpellCard fs in YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(Owner))
        {
            if (fs is Umi)
                return 1;
        }

        return 0;
    }
}
