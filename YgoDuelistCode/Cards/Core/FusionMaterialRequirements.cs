using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Optional stat filters for a requirement-based fusion material slot (combined with AND when multiple flags set).</summary>
public readonly struct FusionMaterialRequirements
{
    public FusionMaterialRequirementFilterMask FilterMask { get; init; }
    public int LevelMin { get; init; }
    public int LevelMax { get; init; }
    public int AtkMin { get; init; }
    public int AtkMax { get; init; }
    public int DefMin { get; init; }
    public int DefMax { get; init; }
    public DuelMonsterAttributeMask Attributes { get; init; }
    public DuelMonsterRaceMask Races { get; init; }

    /// <summary>Dragon race only (e.g. Five-Headed Dragon).</summary>
    public static FusionMaterialRequirements DragonRaceOnly() =>
        new()
        {
            FilterMask = FusionMaterialRequirementFilterMask.Race,
            Races = DuelMonsterRaceMask.Of(DuelMonsterRace.Dragon)
        };
}
