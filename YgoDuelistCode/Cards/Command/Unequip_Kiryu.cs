using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Unequip_Kiryu : Unequip_Union_Base
{
    protected override bool IsMatchingUnionEquip(BaseEquipSpellCard equip) => equip is Kiryu_Union_Equip;

    protected override string CanonicalPortraitPath => ModelDb.Card<Kiryu>().PortraitPath;
}
