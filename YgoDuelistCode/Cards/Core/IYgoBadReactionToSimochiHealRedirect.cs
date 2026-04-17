namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Continuous trap in the Spell/Trap zone that converts enemy creature heals into damage.
/// <see cref="Services.YgoBadReactionToSimochi"/> picks the best face-up copy by multiplier.
/// </summary>
public interface IYgoBadReactionToSimochiHealRedirect
{
    decimal GetEnemyHealRedirectDamageMultiplier();
}
