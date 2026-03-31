using System;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Fusion Summon rules (Extra Deck filter, material destination, target picker) shared by
/// <see cref="FusionSpellCard"/> and field spells such as Fusion Gate.
/// </summary>
public interface IFusionSpellSource
{
    Type FusionTargetMonsterType { get; }
    bool BanishesFusionMaterials { get; }
    bool RequiresPlayerFusionTargetSelection { get; }
}
