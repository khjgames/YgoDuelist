using MegaCrit.Sts2.Core.Entities.Cards;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Marker for monsters that can substitute for exactly one required fusion material.
/// Fusion matching allows at most one such substitute card in a valid recipe.
/// </summary>
public interface IFusionMaterialSubstitute
{
    bool CanSubstituteAsFusionMaterial => true;
}
