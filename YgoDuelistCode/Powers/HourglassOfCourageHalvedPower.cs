using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Timer for Hourglass of Courage: while present, its source card uses halved ATK/DEF via <c>GetSelfStatMultiplier</c>.</summary>
public sealed class HourglassOfCourageHalvedPower : YgoDuelistPower
{
    /// <summary>Power id does not map to this card’s portrait stem by rule; reuse Hourglass of Courage art.</summary>
    protected override string? CardPortraitStemOverride => "hourglass_of_courage";

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-HOURGLASS_OF_COURAGE_HALVED_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-HOURGLASS_OF_COURAGE_HALVED_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        if (Amount > 1m)
        {
            await PowerCmd.ModifyAmount(this, -1m, null, null);
            return;
        }

        await PowerCmd.Remove(this);
    }
}
