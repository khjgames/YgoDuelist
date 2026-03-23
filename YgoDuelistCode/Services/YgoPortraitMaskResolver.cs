using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Chooses portrait mask art: set/face-down, attack position, or skill/defense (and face-up spells/traps).
/// </summary>
public enum YgoPortraitMaskKind
{
    Set,
    Attack,
    Skill,
}

public static class YgoPortraitMaskResolver
{
    /// <summary>
    /// Resolves mask for YGO cards. Non-YGO models should not call this for UI.
    /// </summary>
    public static YgoPortraitMaskKind Resolve(CardModel model)
    {
        if (model is BaseSpellCard spell && (spell.FaceDown || spell.IsSetModeInHand))
            return YgoPortraitMaskKind.Set;
        if (model is BaseTrapCard trap && trap.FaceDown)
            return YgoPortraitMaskKind.Set;

        // Monsters in set / face-down defense use Skill type but need the set mask (not skill portrait shape).
        if (model is AbstractMonsterCard monster && monster.FaceDown)
            return YgoPortraitMaskKind.Set;

        if (model.Type == CardType.Attack)
            return YgoPortraitMaskKind.Attack;

        return YgoPortraitMaskKind.Skill;
    }

    public static bool ShouldApplyPortraitMask(CardModel model) =>
        model is IYgoCard;
}
