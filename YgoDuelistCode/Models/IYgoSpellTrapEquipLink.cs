using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Spell/trap that binds to one field monster like an equip (UI link overlay + registry).
/// </summary>
public interface IYgoSpellTrapEquipLink
{
    BaseMonsterCard? EquipLinkedMonster { get; }

    void SetEquipLinkedMonster(BaseMonsterCard? monster);

    /// <summary>
    /// MP: field monster reference is not serialized; stash the field pet id so equip-link traps can re-bind after state sync.
    /// </summary>
    uint EquipLinkedPetCombatId { get; }

    void SetEquipLinkedPetCombatId(uint petCombatId);

    /// <summary>When this card moves from the spell/trap zone to the graveyard, remove its registry link (default: true).</summary>
    bool DetachSpellTrapEquipLinkOnSpellTrapZoneToGraveyard => true;

    /// <summary>When a link is removed due to zone→graveyard, destroy the linked duel monster if still on the field (default: true).</summary>
    bool DestroyLinkedDuelMonsterOnSpellTrapZoneToGraveyard => true;
}
