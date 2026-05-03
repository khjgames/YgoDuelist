using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Union equip spell cards that register an Unequip command on the host monster's option pile.
/// Implemented on the equip class so hosts like <see cref="Cards.Monster.Done.Normal.Dark_Blade"/> stay generic.
/// </summary>
public interface IYgoUnionEquipUnequipMonsterOption
{
    MonsterCommandCard CreateUnequipMonsterOption(CombatState combatState, Player player);
}
