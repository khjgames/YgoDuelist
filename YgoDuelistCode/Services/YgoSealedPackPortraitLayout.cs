using Godot;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Portrait template and slot layout in 600×846 design pixels (sealed pack art).</summary>
public enum SealedPackPortraitTemplate
{
    A,
    B,
    C,
    D,
    E
}

public readonly record struct SealedPackPortraitSlot(
    SealedPackPortraitTemplate Template,
    Vector2 DesignPosition,
    Vector2 DesignSize,
    string PortraitFileName);

/// <summary>
/// Geometry per <c>Game_Design/Cardpack_Visuals.md</c>; filenames from <see cref="YgoPackTagVisualDefaults.GetTagPortraits"/>.
/// </summary>
public static class YgoSealedPackPortraitLayout
{
    /// <summary>Three Template B slots (298×236).</summary>
    public static SealedPackPortraitSlot[] BuildSingleTagSlots(YgoCardPackTags singleBit)
    {
        string[] p = YgoPackTagVisualDefaults.GetTagPortraits(singleBit);
        return
        [
            new(SealedPackPortraitTemplate.B, new Vector2(35f, 29f), new Vector2(298f, 236f), p[0]),
            new(SealedPackPortraitTemplate.B, new Vector2(149f, 304f), new Vector2(298f, 236f), p[1]),
            new(SealedPackPortraitTemplate.B, new Vector2(260f, 577f), new Vector2(298f, 236f), p[2])
        ];
    }

    /// <summary>Six slots: first three portraits for <paramref name="firstBit"/>, last three for <paramref name="secondBit"/>.</summary>
    public static SealedPackPortraitSlot[] BuildDoubleTagSlots(YgoCardPackTags firstBit, YgoCardPackTags secondBit)
    {
        string[] t1 = YgoPackTagVisualDefaults.GetTagPortraits(firstBit);
        string[] t2 = YgoPackTagVisualDefaults.GetTagPortraits(secondBit);
        return
        [
            new(SealedPackPortraitTemplate.A, new Vector2(6f, 218f), new Vector2(248f, 197f), t1[0]),
            new(SealedPackPortraitTemplate.B, new Vector2(35f, 29f), new Vector2(298f, 236f), t1[1]),
            new(SealedPackPortraitTemplate.A, new Vector2(315f, 31f), new Vector2(248f, 197f), t1[2]),

            new(SealedPackPortraitTemplate.A, new Vector2(345f, 424f), new Vector2(248f, 197f), t2[0]),
            new(SealedPackPortraitTemplate.B, new Vector2(260f, 577f), new Vector2(298f, 236f), t2[1]),
            new(SealedPackPortraitTemplate.A, new Vector2(28f, 603f), new Vector2(248f, 197f), t2[2])
        ];
    }

    /// <summary>Six slots: first three portraits for <paramref name="firstBit"/>, last three for <paramref name="secondBit"/>.</summary>
    public static SealedPackPortraitSlot[] BuildTripleTagSlots(YgoCardPackTags firstBit, YgoCardPackTags secondBit, YgoCardPackTags thirdBit)
    {
        string[] t1 = YgoPackTagVisualDefaults.GetTagPortraits(firstBit);
        string[] t2 = YgoPackTagVisualDefaults.GetTagPortraits(secondBit);
        string[] t3 = YgoPackTagVisualDefaults.GetTagPortraits(thirdBit);
        return
        [
            new(SealedPackPortraitTemplate.A, new Vector2(6f, 218f), new Vector2(248f, 197f), t1[0]),
            new(SealedPackPortraitTemplate.B, new Vector2(35f, 29f), new Vector2(298f, 236f), t1[1]),
            new(SealedPackPortraitTemplate.A, new Vector2(315f, 31f), new Vector2(248f, 197f), t1[2]),
            
            new(SealedPackPortraitTemplate.A, new Vector2(345f, 424f), new Vector2(248f, 197f), t2[0]),
            new(SealedPackPortraitTemplate.B, new Vector2(260f, 577f), new Vector2(298f, 236f), t2[1]),
            new(SealedPackPortraitTemplate.A, new Vector2(28f, 603f), new Vector2(248f, 197f), t2[2]),

            new(SealedPackPortraitTemplate.C, new Vector2(295f, 225f), new Vector2(248f, 197f), t3[0]),
            new(SealedPackPortraitTemplate.D, new Vector2(260f, 577f), new Vector2(298f, 236f), t3[1]),
            new(SealedPackPortraitTemplate.E, new Vector2(54f, 424f), new Vector2(248f, 197f), t3[2])
        ];
    }
}
