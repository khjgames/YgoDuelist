using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Blight X: at end of the player's turn, lose X HP (ignores Block), then remove.</summary>
public sealed class BlightPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-BLIGHT_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-BLIGHT_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Enemy)
            return;

        int stacks = (int)Amount;
        if (stacks <= 0)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            stacks,
            ValueProp.Unblockable | ValueProp.Unpowered,
            dealer: null,
            cardSource: null);

        await PowerCmd.Remove(this);
    }
}
