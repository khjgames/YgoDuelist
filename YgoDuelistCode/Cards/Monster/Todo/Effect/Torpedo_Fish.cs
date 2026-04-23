using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Torpedo_Fish : EffectMonsterCard, IYgoPetDebuffPowerAmountReceivedHook
{
    public Torpedo_Fish()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 10,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fish)
    {
    }

    public bool TryZeroIncomingDebuffPowerAmount(
        ref decimal result,
        CombatState combatState,
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? giver)
    {
        _ = combatState;
        _ = canonicalPower;
        _ = amount;
        _ = giver;
        if (!target.IsPet || target.PetOwner?.Creature == null)
            return false;
        if (!DuelMonsterFieldRegistry.HasSourceCard(target, this))
            return false;
        if (!YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(target.PetOwner).Any(static fs => fs is Umi))
            return false;
        result = 0m;
        return true;
    }
}
