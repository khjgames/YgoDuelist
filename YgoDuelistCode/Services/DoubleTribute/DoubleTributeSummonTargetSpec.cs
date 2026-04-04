using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Which monsters a <see cref="IDoubleTributeMaterial"/> counts double toward when used as tribute.
/// Up to three independent checks: normal-only frame, attribute, race (all enabled flags must pass).
/// </summary>
public readonly struct DoubleTributeSummonTargetSpec
{
    public bool RequiresNormalMonster { get; init; }
    public bool RestrictAttribute { get; init; }
    public DuelMonsterAttribute RequiredAttribute { get; init; }
    public bool RestrictRace { get; init; }
    public DuelMonsterRace RequiredRace { get; init; }

    public bool Matches(BaseMonsterCard summon)
    {
        if (RequiresNormalMonster && summon.YgoCardType != YgoCardType.Monster)
            return false;
        if (RestrictAttribute && summon.DuelMonsterAttribute != RequiredAttribute)
            return false;
        if (RestrictRace && summon.DuelMonsterRace != RequiredRace)
            return false;
        return true;
    }
}
