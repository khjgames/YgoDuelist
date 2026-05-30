using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Player buff: flat ATK and DEF for all your duel monsters this turn (same value, i.e. <see cref="StatEffectTotal"/>-style).
/// </summary>
public sealed class GracefulDicePower : YgoDuelistPower
{
    private static string GracefulDiceIconPath =>
        "graceful_dice.png".CardImagePath().Replace('\\', '/');

    public override string CustomPackedIconPath => GracefulDiceIconPath;

    public override string CustomBigIconPath => GracefulDiceIconPath;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-GRACEFUL_DICE_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-GRACEFUL_DICE_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        await PowerCmd.Remove(this);
    }
}
