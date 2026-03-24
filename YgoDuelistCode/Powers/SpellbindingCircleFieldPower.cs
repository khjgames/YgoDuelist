using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Approximates "cannot attack" by re-applying Weak each turn.</summary>
public sealed class SpellbindingCircleFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SPELLBINDING_CIRCLE_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SPELLBINDING_CIRCLE_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        var cs = Owner.CombatState;
        if (cs == null)
            return;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await PowerCmd.Apply<WeakPower>(e, 1m, Owner, null);
    }
}
