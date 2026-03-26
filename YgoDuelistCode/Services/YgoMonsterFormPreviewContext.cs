namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While true, <see cref="Cards.Core.AbstractMonsterCard.ToggleAttackSkill"/> only cycles Attack/Defense
/// (face-down set when in Defense), not a third hand-effect mode. Used during Cyber Jar reveal preview.
/// </summary>
public static class YgoMonsterFormPreviewContext
{
    public static bool RestrictMonsterToggleToAttackDefenseOnly { get; set; }
}
