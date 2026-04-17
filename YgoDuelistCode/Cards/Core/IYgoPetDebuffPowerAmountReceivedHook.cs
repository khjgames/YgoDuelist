using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Source card for a pet that can cancel incoming debuff power amounts (e.g. under field conditions).
/// <see cref="Patches.HookModifyPowerAmountReceivedTorpedoFishUmiPatch"/>.
/// </summary>
public interface IYgoPetDebuffPowerAmountReceivedHook
{
    /// <summary>
    /// Pre: <paramref name="result"/> is non-zero debuff modification on a pet. Set to zero to ignore debuff.
    /// </summary>
    bool TryZeroIncomingDebuffPowerAmount(
        ref decimal result,
        CombatState combatState,
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? giver);
}
