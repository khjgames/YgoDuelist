using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Allows activating set traps on the same turn they were set.
/// One stack is consumed only when a trap with <see cref="BaseTrapCard.SetThisTurn"/> is actually played.
/// </summary>
public sealed class HastenTrapPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-HASTEN_TRAP_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-HASTEN_TRAP_POWER.description");

    public static bool CanActivateEarlyFromSetThisTurn(BaseTrapCard trap)
    {
        Creature? ownerCreature = trap.Owner?.Creature;
        HastenTrapPower? power = ownerCreature?.GetPower<HastenTrapPower>();
        return power != null && power.Amount >= 1m;
    }

    public static async Task TryConsumeOnEarlySetActivationAsync(BaseTrapCard trap)
    {
        Creature? ownerCreature = trap.Owner?.Creature;
        HastenTrapPower? power = ownerCreature?.GetPower<HastenTrapPower>();
        if (ownerCreature == null || power == null || power.Amount < 1m)
            return;

        await PowerCmd.ModifyAmount(power, -1m, ownerCreature, trap);
        if (power.Amount <= 0m)
            await PowerCmd.Remove(power);
    }
}
