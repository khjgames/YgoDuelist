namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Duel monster attribute, mapped from the original Yu-Gi-Oh! card attributes.
/// Numeric values are stable (keyword / save mapping). The compendium attribute filters use an explicit YGO display order, not enum declaration order.
/// </summary>
public enum DuelMonsterAttribute
{
    Earth,
    Water,
    Fire,
    Wind,
    Light,
    Dark,
    Divine
}
