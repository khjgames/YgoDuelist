using System;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>One fusion material position: named card, requirement filter, or either.</summary>
public readonly struct FusionMaterialSlot
{
    public FusionMaterialSlotMode Mode { get; }
    /// <summary>Non-null for <see cref="FusionMaterialSlotMode.NamedOnly"/> and <see cref="FusionMaterialSlotMode.NamedOrRequirement"/>.</summary>
    public Type? NamedType { get; }
    /// <summary>Requirement data when <see cref="FusionMaterialSlotMode.RequirementOnly"/> or <see cref="FusionMaterialSlotMode.NamedOrRequirement"/>.</summary>
    public FusionMaterialRequirements Req { get; }

    private FusionMaterialSlot(FusionMaterialSlotMode mode, Type? namedType, FusionMaterialRequirements req)
    {
        Mode = mode;
        NamedType = namedType;
        Req = req;
    }

    public static FusionMaterialSlot ForNamed(Type namedType)
    {
        ArgumentNullException.ThrowIfNull(namedType);
        if (!typeof(BaseMonsterCard).IsAssignableFrom(namedType))
            throw new ArgumentException("Named fusion material must be a BaseMonsterCard subclass.", nameof(namedType));
        return new FusionMaterialSlot(FusionMaterialSlotMode.NamedOnly, namedType, default);
    }

    public static FusionMaterialSlot ForRequirement(FusionMaterialRequirements requirements)
    {
        if (requirements.FilterMask == FusionMaterialRequirementFilterMask.None)
            throw new ArgumentException("Requirement-only slot needs at least one active filter.", nameof(requirements));
        return new FusionMaterialSlot(FusionMaterialSlotMode.RequirementOnly, null, requirements);
    }

    public static FusionMaterialSlot ForNamedOrRequirement(Type namedType, FusionMaterialRequirements requirements)
    {
        ArgumentNullException.ThrowIfNull(namedType);
        if (!typeof(BaseMonsterCard).IsAssignableFrom(namedType))
            throw new ArgumentException("Named fusion material must be a BaseMonsterCard subclass.", nameof(namedType));
        if (requirements.FilterMask == FusionMaterialRequirementFilterMask.None)
            throw new ArgumentException("Named-or-requirement slot needs requirement filters.", nameof(requirements));
        return new FusionMaterialSlot(FusionMaterialSlotMode.NamedOrRequirement, namedType, requirements);
    }
}
