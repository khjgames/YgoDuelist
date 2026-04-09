using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Your duel monsters have extra Max HP from Fortified Beasts (tracked in <see cref="YgoDuelistPassivePowerState"/>).
/// </summary>
public sealed class FortifiedBeastsPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-FORTIFIED_BEASTS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FORTIFIED_BEASTS_POWER.description");

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        int delta = cardSource is { IsUpgraded: true } ? 3 : 2;
        Player? player = Owner.Player;
        if (player?.PlayerCombatState == null || player.Creature == null)
        {
            await base.AfterApplied(applier, cardSource);
            return;
        }

        YgoDuelistPassivePowerState.AddFortifiedBeastsBonus(player, delta);
        await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);

        await base.AfterApplied(applier, cardSource);
    }
}
