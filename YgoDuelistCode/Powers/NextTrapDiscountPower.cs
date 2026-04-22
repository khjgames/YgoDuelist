using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Powers;

public sealed class NextTrapDiscountPower : NextActivatableEnergyDiscountPower
{
    protected override string? CardPortraitStemOverride => "fake_trap";

    protected override decimal EnergyDiscountPerActivation => 1m;

    protected override bool MatchesDiscountCard(CardModel card) => card is BaseTrapCard;

    public override LocString Title => new("powers", "YGODUELIST-NEXT_TRAP_DISCOUNT_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-NEXT_TRAP_DISCOUNT_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-NEXT_TRAP_DISCOUNT_POWER.smartDescription";
}
