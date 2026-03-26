using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While <see cref="Cards.Trap.Todo.Continuos.Talisman_of_Spell_Sealing"/> is active: once per turn at turn start, gain 2 Artifact until end of turn.</summary>
public sealed class TalismanSpellSealingFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-TALISMAN_SPELL_SEALING_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-TALISMAN_SPELL_SEALING_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await YgoSealmasterMeiseiGate.DestroyTalismansIfNoSealmaster(player);
        if (!YgoSealmasterMeiseiGate.HasFaceUpSealmaster(player))
            return;

        await PowerCmd.Apply<ArtifactPower>(Owner, Amount, Owner, null);
        await PowerCmd.Apply<YgoScheduledArtifactRemovalPower>(Owner, Amount, Owner, null);
    }
}
