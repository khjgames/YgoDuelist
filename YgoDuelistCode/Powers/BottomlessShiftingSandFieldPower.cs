using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>End-of-turn sand damage to enemies while the trap is active.</summary>
public sealed class BottomlessShiftingSandFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-BOTTOMLESS_SHIFTING_SAND_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-BOTTOMLESS_SHIFTING_SAND_FIELD_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        var cs = Owner.CombatState;
        if (cs == null)
            return;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await CreatureCmd.Damage(choiceContext, e, 4m, ValueProp.Unpowered, Owner, null);
    }
}
