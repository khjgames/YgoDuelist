using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>
/// Graveyard trigger: optional activation then Special Summon 1 Insect from hand — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoPinchHopperGraveyard"/>.
/// </summary>
public sealed class Pinch_Hopper : EffectMonsterCard
{
    public Pinch_Hopper()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 10,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

}
