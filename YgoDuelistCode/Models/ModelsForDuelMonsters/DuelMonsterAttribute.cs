namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Duel monster attribute, mapped from the original Yu-Gi-Oh! card attributes.
/// Not yet used mechanically, but stored so aura / support effects can key off it.
/// </summary>
public enum DuelMonsterAttribute
{
    Earth,
    Water,
    Fire,
    Wind,
    Light,
    Dark
}
