using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>
/// Draw 1 when this monster’s attack deals unblocked damage to an enemy — handled in <see cref="Relics.GraveyardRelic.AfterAttack"/>.
/// </summary>
public sealed class Masked_Sorcerer : EffectMonsterCard
{
    public Masked_Sorcerer()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 9,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

}
