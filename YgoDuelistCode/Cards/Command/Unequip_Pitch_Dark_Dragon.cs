using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

public sealed class Unequip_Pitch_Dark_Dragon : Unequip_Union_Base
{
    protected override bool IsMatchingUnionEquip(BaseEquipSpellCard equip) => equip is Pitch_Dark_Dragon_Union_Equip;

    protected override string CanonicalPortraitPath => ModelDb.Card<Pitch_Dark_Dragon>().PortraitPath;
}
