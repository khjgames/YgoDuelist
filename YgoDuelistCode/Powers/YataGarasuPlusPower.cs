using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// From upgraded <see cref="Yata_Garasu"/> attacks: at the start of your next turn, gain 1 Conduit and 1 Energy per stack, then remove.
/// </summary>
public sealed class YataGarasuPlusPower : YgoDuelistPower
{
    public override bool IsInstanced => true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-YATA_GARASU_POWER_PLUS.title");

    public override LocString Description => new("powers", "YGODUELIST-YATA_GARASU_POWER_PLUS.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-YATA_GARASU_POWER_PLUS.smartDescription";

    protected override string? CardPortraitStemOverride => "yata_garasu";

    private bool _pendingNextTurnStart;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _pendingNextTurnStart = true;
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;
        if (player != Owner.Player || !_pendingNextTurnStart)
            return;

        _pendingNextTurnStart = false;

        int n = (int)Amount;
        if (n > 0)
        {
            for (int i = 0; i < n; i++)
                await PlayerCmd.GainStars(1, player);
            await PlayerCmd.GainEnergy(n, player);
        }

        await PowerCmd.Remove(this);
    }
}
