using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Standby burn while <see cref="Cards.Spell.Todo.Continuos.Burning_Land"/> is active.</summary>
public sealed class BurningLandFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-BURNING_LAND_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-BURNING_LAND_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await CreatureCmd.Damage(choiceContext, Owner, 5m, ValueProp.Unpowered, null, null);

        var cs = Owner.CombatState;
        if (cs == null)
            return;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await CreatureCmd.Damage(choiceContext, e, 5m, ValueProp.Unpowered, Owner, null);
    }
}
