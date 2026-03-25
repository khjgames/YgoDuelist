using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// While <see cref="Cards.Spell.Todo.Continuos.Stumbling"/> is active: on each turn (start of yours and after enemies act), enemies lose 1 temporary Strength.
/// </summary>
public sealed class StumblingFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-STUMBLING_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-STUMBLING_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await ApplyStumblingDebuffAsync();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Enemy || Owner.Side != CombatSide.Player)
            return;

        await ApplyStumblingDebuffAsync();
    }

    private async Task ApplyStumblingDebuffAsync()
    {
        var cs = Owner.CombatState;
        if (cs == null)
            return;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await PowerCmd.Apply<YgoTemporaryStrengthLossPower>(e, 1m, Owner, null);
    }
}
