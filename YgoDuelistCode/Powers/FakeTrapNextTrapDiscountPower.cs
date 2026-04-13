using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Your next Trap activation costs 1 less Energy (consumed when you play a Trap).</summary>
public sealed class FakeTrapNextTrapDiscountPower : YgoDuelistPower
{
    protected override string? CardPortraitStemOverride => "fake_trap";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-FAKE_TRAP_NEXT_TRAP_DISCOUNT_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FAKE_TRAP_NEXT_TRAP_DISCOUNT_POWER.description");

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner)
            return false;
        if (card is not BaseTrapCard)
            return false;
        if (!IsSpellTrapActivatablePile(card.Pile?.Type))
            return false;
        if (originalCost < 1m)
            return false;
        modifiedCost = originalCost - 1m;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (card.Owner?.Creature != Owner)
            return;
        if (card is not BaseTrapCard)
            return;
        if (!IsSpellTrapActivatablePile(card.Pile?.Type))
            return;

        await PowerCmd.Remove(this);
        YgoSpellTrapDiscountEnergyRefresh.ForPlayer(card.Owner);
    }

    private static bool IsSpellTrapActivatablePile(PileType? pile) =>
        pile == PileType.Hand || pile == PileType.Play || pile == SpellTrapZonePile.CustomType;
}
