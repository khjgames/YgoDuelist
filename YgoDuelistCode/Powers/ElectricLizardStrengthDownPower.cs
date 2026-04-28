using BaseLib.Abstracts;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Enemy killer debuff from Electric Lizard: silent <see cref="StrengthPower"/> penalty for two of this creature's turn ends.
/// </summary>
public sealed class ElectricLizardStrengthDownPower : YgoDuelistPower, ICustomPower
{
    private const int TurnEndsTotal = 2;

    private int _turnEndsRemaining;
    private decimal _strPenalty;

    public override bool IsInstanced => true;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-ELECTRIC_LIZARD_STRENGTH_DOWN_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ELECTRIC_LIZARD_STRENGTH_DOWN_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-ELECTRIC_LIZARD_STRENGTH_DOWN_POWER.smartDescription";

    string? ICustomPower.CustomPackedIconPath => "electric_lizard.png".CardImagePath();

    string? ICustomPower.CustomBigIconPath => "electric_lizard.png".CardImagePath();

    string? ICustomPower.CustomBigBetaIconPath => null;

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _strPenalty = amount;
        _turnEndsRemaining = TurnEndsTotal;
        await PowerCmd.Apply<StrengthPower>(target, -_strPenalty, applier, cardSource, silent: true);
        await base.BeforeApplied(target, amount, applier, cardSource);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        Flash();
        _turnEndsRemaining--;
        if (_turnEndsRemaining > 0)
            return;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(Owner, _strPenalty, Owner, null, silent: true);
    }
}