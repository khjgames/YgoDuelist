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

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>End-of-turn burn to enemies while <see cref="Cards.Spell.Todo.Continuos.Burning_Land"/> is active (poison timing).</summary>
public sealed class BurningLandFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-BURNING_LAND_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-BURNING_LAND_FIELD_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        var cs = Owner.CombatState;
        if (cs == null)
            return;

        Player? pl = Owner.Player;
        Burning_Land? src = pl == null
            ? null
            : SpellTrapZonePile.CustomType.GetPile(pl)?.Cards.OfType<Burning_Land>().FirstOrDefault();
        decimal dmg = src != null ? src.DynamicVars["Mgc"].BaseValue : 5m;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await CreatureCmd.Damage(choiceContext, e, dmg, ValueProp.Unpowered, Owner, src);
    }
}
