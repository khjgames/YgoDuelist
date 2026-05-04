using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Bold O overlays; heavier ✖ exclude overlay (black weight + thick outline).</summary>
internal static class YgoCardLibraryOverlayGlyphStyle
{
    /// <summary>OpenType <c>wght</c> axis tag (big-endian four-char).</summary>
    const long WghtAxisTag = 2003265652L;

    internal static void ApplyBoldOverlayGlyph(Label glyph, Font? baseFont)
    {
        ApplyWeightedGlyph(glyph, baseFont, weight: 700, fontSize: 26, outlineSize: 6);
    }

    /// <summary>Exclude tick: thicker than <see cref="ApplyBoldOverlayGlyph"/> (AND “O” unchanged).</summary>
    internal static void ApplyExcludeOverlayGlyph(Label glyph, Font? baseFont)
    {
        ApplyWeightedGlyph(glyph, baseFont, weight: 900, fontSize: 30, outlineSize: 10);
    }

    static void ApplyWeightedGlyph(Label glyph, Font? baseFont, float weight, int fontSize, int outlineSize)
    {
        Font? resolved = baseFont ?? ThemeDB.FallbackFont;
        Font? font = TryBoldVariation(resolved, weight) ?? resolved;
        glyph.AddThemeFontOverride("font", font);
        glyph.AddThemeFontSizeOverride("font_size", fontSize);
        glyph.AddThemeColorOverride("font_color", StsColors.gold);
        glyph.AddThemeConstantOverride("outline_size", outlineSize);
        glyph.AddThemeColorOverride("font_outline_color", StsColors.rewardLabelGoldOutline);
    }

    static Font? TryBoldVariation(Font? baseFont, float weight)
    {
        if (baseFont == null)
            return null;
        var axes = new Godot.Collections.Dictionary();
        axes[WghtAxisTag] = weight;
        return new FontVariation { BaseFont = baseFont, VariationOpentype = axes };
    }
}
