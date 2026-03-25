using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Player marker: Curse of Anubis is active this turn (new Effect Monster summons inherit the pet debuff).</summary>
public sealed class YgoCurseOfAnubisPlayerMarkerPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-YGO_CURSE_OF_ANUBIS_PLAYER_MARKER_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-YGO_CURSE_OF_ANUBIS_PLAYER_MARKER_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;
        await PowerCmd.Remove(this);
    }
}
