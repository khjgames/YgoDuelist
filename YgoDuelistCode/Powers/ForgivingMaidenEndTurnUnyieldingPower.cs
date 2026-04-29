using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Same spill-DFY floor behavior as <see cref="UnyieldingPower"/>, lasts until end of your turn (stripped in <see cref="Services.MonsterCommandRegistry.ResolveOwnerTurnEndFieldCleanupAsync"/>).
/// When both this and <see cref="UnyieldingPower"/> apply, <see cref="UnyieldingPower"/> defers so this hook applies <c>max</c> of the two floors.
/// </summary>
public sealed class ForgivingMaidenEndTurnUnyieldingPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-UNYIELDING_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FORGIVING_MAIDEN_END_TURN_UNYIELDING_POWER.description");

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        decimal v = base.ModifyHpLostAfterOsty(target, amount, props, dealer, cardSource);
        if (target != Owner || v <= 0m)
            return v;
        if (!target.IsPet || dealer == null || dealer.Side != CombatSide.Enemy)
            return v;
        if (!props.HasFlag(ValueProp.Move) || !props.IsPoweredAttack())
            return v;
        if (target.Monster is not DuelMonsterModel)
            return v;

        int maidenFloor = (int)Amount;
        UnyieldingPower? perm = target.GetPower<UnyieldingPower>();
        int permFloor = perm != null ? (int)perm.Amount : 0;
        int floor = System.Math.Max(maidenFloor, permFloor);
        if (floor <= 0)
            return v;

        int maxLoss = System.Math.Max(0, target.CurrentHp - floor);
        return System.Math.Min(v, maxLoss);
    }
}
