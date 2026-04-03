namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Equip-link trap/spell whose <see cref="IYgoSpellTrapEquipLink"/> applies ATK/DEF multipliers and/or command energy discounts to the linked monster.
/// </summary>
public interface IYgoSpellTrapEquipLinkStatEffect
{
    bool IsSpellTrapEquipLinkStatEffectActive { get; }

    StatEffectTotalMultiplier GetSpellTrapEquipLinkStatMultiplier();

    int GetSpellTrapEquipLinkAttackPlayEnergyDiscount();

    int GetSpellTrapEquipLinkDefensePlayEnergyDiscount();
}
