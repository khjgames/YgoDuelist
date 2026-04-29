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
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

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
        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(pl);
        Burning_Land? src = zone == null
            ? null
            : YgoMpCombatOrder.FirstCardWhereStable(zone.Cards, c => c is Burning_Land) as Burning_Land;
        decimal dmg = src != null ? src.DynamicVars["Mgc"].BaseValue : 5m;

        foreach (Creature e in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
            await CreatureCmd.Damage(choiceContext, e, dmg, ValueProp.Unpowered, Owner, src);
    }
}
