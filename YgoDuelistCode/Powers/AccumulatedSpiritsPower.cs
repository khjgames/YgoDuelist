using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// At the end of your turn, if you lost 2+ duel monsters from the field this turn, gain 1 Strength and 1 Dexterity.
/// </summary>
public sealed class AccumulatedSpiritsPower : YgoDuelistPower
{
    private const int LossThreshold = 2;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-ACCUMULATED_SPIRITS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ACCUMULATED_SPIRITS_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;
        YgoDuelistPassivePowerState.ResetAccumulatedSpiritsForTurn(player);
        await Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        Player? player = Owner.Player;
        if (player?.Creature == null)
            return;

        int lost = YgoDuelistPassivePowerState.GetAccumulatedSpiritsLossesThisTurn(player);
        if (lost < LossThreshold)
            return;

        await PowerCmd.Apply<StrengthPower>(player.Creature, 1m, player.Creature, null);
        await PowerCmd.Apply<DexterityPower>(player.Creature, 1m, player.Creature, null);
    }
}
