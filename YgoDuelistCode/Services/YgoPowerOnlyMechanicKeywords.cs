using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YGO mechanic keywords that are explained only through <see cref="MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower{T}"/>
/// (power icons and smart descriptions). Do not add these to <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardModel.Keywords"/>:
/// the engine generates plain-keyword hover tips from every chip, which duplicates the power tips.
/// </summary>
public static class YgoPowerOnlyMechanicKeywords
{
    public static readonly CardKeyword Doomed = (CardKeyword)20048;
    public static readonly CardKeyword SlifersPressure = (CardKeyword)20049;
    public static readonly CardKeyword SlifersPressurePlus = (CardKeyword)20050;
    public static readonly CardKeyword Rebirth = (CardKeyword)20052;
    public static readonly CardKeyword PumpkingRitual = (CardKeyword)20056;
    public static readonly CardKeyword NecroticEvolution = (CardKeyword)20057;
    public static readonly CardKeyword NecroticRitual = (CardKeyword)20058;
    public static readonly CardKeyword ChaoticEvolution = (CardKeyword)20059;

    private static readonly HashSet<CardKeyword> OmitFromChips =
    [
        Doomed,
        SlifersPressure,
        SlifersPressurePlus,
        Rebirth,
        PumpkingRitual,
        NecroticEvolution,
        NecroticRitual,
        ChaoticEvolution,
    ];

    /// <summary>Strip power-only mechanic ids after composing keyword lists (e.g. archetype unions that might include these).</summary>
    public static IEnumerable<CardKeyword> WithoutOmittedMechanicChips(IEnumerable<CardKeyword> keywords) =>
        keywords.Where(k => !OmitFromChips.Contains(k));
}
