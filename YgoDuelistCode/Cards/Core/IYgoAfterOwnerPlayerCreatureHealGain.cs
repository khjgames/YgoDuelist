using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Field monster that reacts when its owner’s player creature gains HP via <see cref="MegaCrit.Sts2.Core.Commands.CreatureCmd.Heal"/>.
/// </summary>
public interface IYgoAfterOwnerPlayerCreatureHealGain
{
    Task ReactToOwnerPlayerHpGainAfterHealAsync(
        Player owningPlayer,
        Creature healedCreature,
        decimal hpBeforeHeal,
        decimal gainedHp,
        CombatState combatState,
        int deterministicTick);
}
