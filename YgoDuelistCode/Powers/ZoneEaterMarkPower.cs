using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Inflicted by <see cref="Cards.Monster.Todo.Effect.Zone_Eater"/>: after 5 of your turn ends, this enemy takes 20 damage.</summary>
public sealed class ZoneEaterMarkPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-ZONE_EATER_MARK_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ZONE_EATER_MARK_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player)
            return;
        if (Owner.Side != CombatSide.Enemy || !Owner.IsAlive)
            return;
        if (Amount <= 0m)
            return;

        if (Amount <= 1m)
        {
            await CreatureCmd.Damage(choiceContext, Owner, 20m, ValueProp.Unpowered | ValueProp.SkipHurtAnim, dealer: null, cardSource: null);
            await PowerCmd.Remove(this);
        }
        else
            await PowerCmd.ModifyAmount(this, -1m, null, null);
    }
}
