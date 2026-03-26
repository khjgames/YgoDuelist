using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Revised: same ATK/DEF/level as Dark Blade (fusion material).</summary>
public sealed class Pitch_Dark_Dragon : EffectMonsterCard
{
    public Pitch_Dark_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 18,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

}
