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
        // Match hand / zone presentation: FaceDown is cleared outside hand & spell zone (compendium, deck grid) but
        // traps still use set portrait chrome via ShouldUseFaceDownPresentation — same as YgoSetCardVisualHelper.
        if (model is BaseTrapCard trap && trap.ShouldUseFaceDownPresentation())
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
