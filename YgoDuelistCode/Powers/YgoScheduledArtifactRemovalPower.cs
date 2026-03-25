using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>At end of turn, removes up to <see cref="PowerModel.Amount"/> stacks of <see cref="ArtifactPower"/> then removes itself (Talisman of Spell Sealing).</summary>
public sealed class YgoScheduledArtifactRemovalPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-YGO_SCHEDULED_ARTIFACT_REMOVAL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-YGO_SCHEDULED_ARTIFACT_REMOVAL_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        int n = (int)Amount;
        for (int i = 0; i < n; i++)
        {
            ArtifactPower? art = Owner.GetPower<ArtifactPower>();
            if (art == null || art.Amount <= 0m)
                break;
            await PowerCmd.Decrement(art);
        }

        await PowerCmd.Remove(this);
    }
}
