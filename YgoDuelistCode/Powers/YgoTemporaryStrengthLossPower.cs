using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Temporary Strength loss (restored at end of creature's side turn).</summary>
public sealed class YgoTemporaryStrengthLossPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-YGO_TEMPORARY_STRENGTH_LOSS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-YGO_TEMPORARY_STRENGTH_LOSS_POWER.description");

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource) =>
        await PowerCmd.Apply<StrengthPower>(target, -amount, applier, cardSource, silent: true);

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        if (Amount <= 0)
            return;
        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null, silent: true);
    }
}

