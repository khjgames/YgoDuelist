using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Continuous <see cref="Dark_Snake_Syndrome"/>: end-of-player-turn damage to marked target; counter doubles (max 64).</summary>
public sealed class DarkSnakeSyndromeFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        Player? pl = Owner.Player;
        if (pl == null)
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        if (zone == null || !zone.Cards.OfType<Dark_Snake_Syndrome>().Any())
        {
            YgoDarkSnakeSyndromeTargetState.Remove(pl);
            await PowerCmd.Remove(this);
            return;
        }

        if (!YgoDarkSnakeSyndromeTargetState.TryGet(pl, out uint targetId))
            return;

        CombatState? cs = Owner.CombatState;
        Creature? target = cs?.HittableEnemies.FirstOrDefault(c => c.IsAlive && c.CombatId == targetId);
        if (target == null || !target.IsAlive)
            return;

        Dark_Snake_Syndrome? src = zone.Cards.OfType<Dark_Snake_Syndrome>().FirstOrDefault();
        decimal dmg = Amount;
        if (dmg > 0m)
            await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, Owner, src);

        decimal next = System.Math.Min(dmg * 2m, 64m);
        decimal delta = next - Amount;
        if (delta != 0m)
            await PowerCmd.ModifyAmount(this, delta, null, null);
    }
}
