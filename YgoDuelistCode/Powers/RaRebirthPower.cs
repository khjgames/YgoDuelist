using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>From The Winged Dragon of Ra execute kills: at combat end (before Doomed damage), removes Doom equal to these stacks, then falls off.</summary>
public sealed class RaRebirthPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-RA_REBIRTH_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-RA_REBIRTH_POWER.description");

    protected override string? CardPortraitStemOverride => "the_winged_dragon_of_ra";

    public static async Task ResolveCombatEndBeforeDoomedAsync(Player player)
    {
        if (player.Creature?.GetPower<RaRebirthPower>() is not { } rebirth)
            return;

        int stacks = rebirth.Amount;
        if (stacks <= 0)
        {
            await PowerCmd.Remove(rebirth);
            return;
        }

        if (player.Creature.GetPower<DoomPower>() is { } doomPow)
        {
            int doom = doomPow.Amount;
            int remove = System.Math.Min(stacks, doom);
            if (remove > 0)
            {
                int newDoom = doom - remove;
                if (newDoom <= 0)
                    await PowerCmd.Remove(doomPow);
                else
                    doomPow.SetAmount(newDoom);
            }
        }

        await PowerCmd.Remove(rebirth);
    }
}
