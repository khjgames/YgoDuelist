using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Renders YGO level stars and attribute icon on monster cards (under title banner, right-aligned).
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoMonsterLevelStripPatch
{
    private const string StarsStripPath = "YgoDuelist/images/card_frames/12_stars.png";
    private const string AttributeIconFolder = "YgoDuelist/images/card_frames/Attribute";
    private const string StripNodeName = "YgoLevelStarsStrip";
    private const string AttributeNodeName = "YgoAttributeIcon";

    /// <summary>Vertical strip height in card-local space (same anchor mode as title banner).</summary>
    private const float StripHeightPx = 20f;

    /// <summary>
    /// Shift the whole strip vertically after placing its top at the banner bottom.
    /// Negative = move up (toward the card top / into the banner); positive = move down.
    /// </summary>
    private const float StripVerticalNudgePx = -35f;

    /// <summary>
    /// Shift the strip horizontally after right-aligning to the banner. Positive = move left (toward card center).
    /// </summary>
    private const float StripHorizontalNudgePx = 48f;

    /// <summary>Gap between the bottom of the level strip and the top of the attribute icon.</summary>
    private const float AttributeGapBelowLevelStripPx = 2f;

    /// <summary>Attribute icon box height (width follows texture aspect).</summary>
    private const float AttributeIconHeightPx = 24f;

    /// <summary>Extra vertical offset after gap below level strip (negative = further up).</summary>
    private const float AttributeExtraVerticalNudgePx = 0f;

    /// <summary>Horizontal nudge for attribute (positive = move left), same sense as <see cref="StripHorizontalNudgePx"/>.</summary>
    private const float AttributeHorizontalNudgePx = 48f;

    private static Texture2D? _stripTexture;
    private static AtlasTexture[]? _atlasesByLevel;
    private static readonly Dictionary<DuelMonsterAttribute, Texture2D?> _attributeTextures = new();

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        Control body = __instance.Body;
        var strip = body.GetNodeOrNull<TextureRect>(StripNodeName);
        var attributeIcon = body.GetNodeOrNull<TextureRect>(AttributeNodeName);

        CardModel? model = __instance.Model;
        if (model == null || model.Rarity == CardRarity.Ancient || model is not AbstractMonsterCard monster)
        {
            strip?.Hide();
            attributeIcon?.Hide();
            return;
        }

        _stripTexture ??= ResourceLoader.Load<Texture2D>(StarsStripPath, null, ResourceLoader.CacheMode.Reuse);
        if (_stripTexture == null)
        {
            strip?.Hide();
        }
        else
        {
            EnsureAtlases(_stripTexture);
        }

        var banner = body.GetNodeOrNull<TextureRect>("%TitleBanner");
        if (banner == null)
        {
            strip?.Hide();
            attributeIcon?.Hide();
            return;
        }

        if (strip == null)
        {
            strip = new TextureRect
            {
                Name = StripNodeName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both,
            };
            body.AddChild(strip);
            body.MoveChild(strip, banner.GetIndex() + 1);
        }

        int level = Mathf.Clamp(monster.DuelMonsterLevel, 1, 12);
        if (_stripTexture == null || _atlasesByLevel == null || level < 1 || level > 12)
        {
            strip?.Hide();
        }
        else
        {
            strip!.Texture = _atlasesByLevel[level - 1];
            strip.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            strip.StretchMode = TextureRect.StretchModeEnum.Scale;
            ApplyLayoutRightAnchoredBelowBanner(strip, banner, level);
            strip.Show();
        }

        TextureRect attrNode = EnsureAttributeIconNode(body, banner, strip);
        UpdateAttributeIcon(attrNode, monster, banner);
    }

    private static TextureRect EnsureAttributeIconNode(Control body, TextureRect banner, TextureRect? strip)
    {
        var attr = body.GetNodeOrNull<TextureRect>(AttributeNodeName);
        if (attr != null)
            return attr;

        attr = new TextureRect
        {
            Name = AttributeNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
        body.AddChild(attr);
        int insertAt = strip != null ? strip.GetIndex() + 1 : banner.GetIndex() + 1;
        body.MoveChild(attr, insertAt);
        return attr;
    }

    private static void UpdateAttributeIcon(TextureRect attr, AbstractMonsterCard monster, TextureRect banner)
    {
        Texture2D? tex = GetAttributeTexture(monster.DuelMonsterAttribute);
        if (tex == null)
        {
            attr.Hide();
            return;
        }

        attr.Texture = tex;
        attr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        attr.StretchMode = TextureRect.StretchModeEnum.Scale;

        attr.AnchorLeft = 0.5f;
        attr.AnchorRight = 0.5f;
        attr.AnchorTop = banner.AnchorTop;
        attr.AnchorBottom = banner.AnchorBottom;

        float levelStripTop = banner.OffsetBottom + StripVerticalNudgePx;
        float top = levelStripTop + StripHeightPx + AttributeGapBelowLevelStripPx + AttributeExtraVerticalNudgePx;
        attr.OffsetTop = top;
        attr.OffsetBottom = top + AttributeIconHeightPx;

        Vector2 szf = tex.GetSize();
        int tw = (int)szf.X;
        int th = (int)szf.Y;
        if (tw <= 0 || th <= 0)
        {
            attr.Hide();
            return;
        }

        float displayW = AttributeIconHeightPx * (tw / (float)th);
        float bannerW = banner.OffsetRight - banner.OffsetLeft;
        if (displayW > bannerW)
            displayW = bannerW;

        float right = banner.OffsetRight - AttributeHorizontalNudgePx;
        attr.OffsetRight = right;
        attr.OffsetLeft = right - displayW;
        attr.Show();
    }

    private static Texture2D? GetAttributeTexture(DuelMonsterAttribute attribute)
    {
        if (_attributeTextures.TryGetValue(attribute, out Texture2D? cached))
            return cached;

        string path = $"{AttributeIconFolder}/{attribute.ToString().ToLowerInvariant()}.png";
        Texture2D? loaded = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        _attributeTextures[attribute] = loaded;
        return loaded;
    }

    private static void EnsureAtlases(Texture2D strip)
    {
        if (_atlasesByLevel != null)
            return;

        Vector2 szf = strip.GetSize();
        int tw = (int)szf.X;
        int th = (int)szf.Y;
        if (tw <= 0 || th <= 0)
            return;

        int cellW = tw / 12;
        if (cellW <= 0)
            return;

        _atlasesByLevel = new AtlasTexture[12];
        for (int i = 0; i < 12; i++)
        {
            int regionW = cellW * (i + 1);
            var atlas = new AtlasTexture
            {
                Atlas = strip,
                Region = new Rect2I(0, 0, regionW, th),
            };
            _atlasesByLevel[i] = atlas;
        }
    }

    /// <summary>
    /// Same vertical band as the title banner; horizontal strip is width = aspect-fit for current level,
    /// right edge aligned with the banner’s right edge (YGO-style).
    /// </summary>
    private static void ApplyLayoutRightAnchoredBelowBanner(TextureRect strip, TextureRect banner, int level)
    {
        strip.AnchorLeft = 0.5f;
        strip.AnchorRight = 0.5f;
        strip.AnchorTop = banner.AnchorTop;
        strip.AnchorBottom = banner.AnchorBottom;

        float top = banner.OffsetBottom + StripVerticalNudgePx;
        strip.OffsetTop = top;
        strip.OffsetBottom = top + StripHeightPx;

        if (_stripTexture == null)
            return;

        Vector2 szf = _stripTexture.GetSize();
        int tw = (int)szf.X;
        int th = (int)szf.Y;
        if (tw <= 0 || th <= 0)
            return;

        int cellW = tw / 12;
        if (cellW <= 0)
            return;

        int regionW = cellW * Mathf.Clamp(level, 1, 12);
        float displayW = StripHeightPx * (regionW / (float)th);

        float bannerW = banner.OffsetRight - banner.OffsetLeft;
        if (displayW > bannerW)
            displayW = bannerW;

        float right = banner.OffsetRight - StripHorizontalNudgePx;
        strip.OffsetRight = right;
        strip.OffsetLeft = right - displayW;
    }
}
