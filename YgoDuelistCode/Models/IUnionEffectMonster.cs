using System;

namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Monster that can use the Union Equip command and bind to a union equip spell.
/// </summary>
public interface IUnionEffectMonster
{
    Type UnionEquipSpellType { get; }
}
