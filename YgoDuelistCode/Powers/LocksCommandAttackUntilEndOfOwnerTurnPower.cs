using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While active on this duel pet, <see cref="Cards.Monster.Done.Effect.Chaos_Sorcerer"/> cannot use Command Attack (cleared at end of that side's turn).</summary>
public sealed class LocksCommandAttackUntilEndOfOwnerTurnPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-LOCKS_COMMAND_ATTACK_UNTIL_END_OF_OWNER_TURN_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-LOCKS_COMMAND_ATTACK_UNTIL_END_OF_OWNER_TURN_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-LOCKS_COMMAND_ATTACK_UNTIL_END_OF_OWNER_TURN_POWER.smartDescription";

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        await PowerCmd.Remove(this);
    }
}
