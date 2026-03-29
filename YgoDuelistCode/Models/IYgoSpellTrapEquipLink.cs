using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Continuous spell/trap in the Spell/Trap zone that binds to one field monster like an equip (UI link overlay + mutual leave-field).
/// </summary>
public interface IYgoSpellTrapEquipLink
{
    BaseMonsterCard? EquipLinkedMonster { get; }

    void SetEquipLinkedMonster(BaseMonsterCard? monster);
}
