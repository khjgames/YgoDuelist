using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>After Cold Wave resolves: you cannot play or Set other Spell/Trap cards for the rest of this turn.</summary>
public sealed class ColdWaveSpellTrapLockPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-COLD_WAVE_SPELL_TRAP_LOCK_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-COLD_WAVE_SPELL_TRAP_LOCK_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        await PowerCmd.Remove(this);
    }
}
