using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Marker from The Winged Dragon of Ra: at combat end, take damage equal to your Doom stacks.</summary>
public sealed class RaDoomedPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-RA_DOOMED_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-RA_DOOMED_POWER.description");

    public static async Task ResolveCombatEndDamageAsync(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature?.GetPower<RaDoomedPower>() is not { } doomed)
            return;

        decimal doom = player.Creature.GetPowerAmount<DoomPower>();
        if (doom > 0m)
        {
            await CreatureCmd.Damage(
                ctx,
                player.Creature,
                (int)doom,
                ValueProp.Unblockable | ValueProp.Unpowered,
                dealer: null,
                cardSource: null);
        }

        await PowerCmd.Remove(doomed);
    }
}
