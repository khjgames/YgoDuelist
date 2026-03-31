using MegaCrit.Sts2.Core.Entities.Cards;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Marker for monsters that can substitute for exactly one <b>named</b> fusion material slot
/// (<see cref="FusionMaterialSlotMode.NamedOnly"/> or the named branch of <see cref="FusionMaterialSlotMode.NamedOrRequirement"/>).
/// At most one substitute may appear in a full fusion recipe.
/// Requirement-based slots (<see cref="FusionMaterialSlotMode.RequirementOnly"/> and the requirement branch of
/// <see cref="FusionMaterialSlotMode.NamedOrRequirement"/>) never accept substitutes; the monster must satisfy
/// <see cref="FusionMaterialRequirements"/> for that slot.
/// </summary>
public interface IFusionMaterialSubstitute
{
    bool CanSubstituteAsFusionMaterial => true;
}
