namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// ZGO card type for frame/background styling (spell, trap, monster, etc.).
/// </summary>
public enum YgoCardType
{
    Spell,
    Trap,
    Monster,
    EffectMonster,
    FusionMonster,
    RitualMonster
}

/// <summary>
/// Implement on card models that use ZGO-style card frames/backgrounds.
/// </summary>
public interface IYgoCard
{
    YgoCardType YgoCardType { get; }
}
