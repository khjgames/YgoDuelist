using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Renders YGO level stars (cropped from 12_stars.png) in a row directly under the title banner for monster cards.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoMonsterLevelStripPatch
{
    private const string StarsStripPath = "YgoDuelist/images/card_frames/12_stars.png";
    private const string StripNodeName = "YgoLevelStarsStrip";

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

    private static Texture2D? _stripTexture;
    private static AtlasTexture[]? _atlasesByLevel;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        Control body = __instance.Body;
        var strip = body.GetNodeOrNull<TextureRect>(StripNodeName);

        CardModel? model = __instance.Model;
        if (model == null || model.Rarity == CardRarity.Ancient || model is not AbstractMonsterCard monster)
        {
            strip?.Hide();
            return;
        }

        _stripTexture ??= ResourceLoader.Load<Texture2D>(StarsStripPath, null, ResourceLoader.CacheMode.Reuse);
        if (_stripTexture == null)
        {
            strip?.Hide();
            return;
        }

        EnsureAtlases(_stripTexture);

        var banner = body.GetNodeOrNull<TextureRect>("%TitleBanner");
        if (banner == null)
        {
            strip?.Hide();
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
        if (_atlasesByLevel == null || level < 1 || level > 12)
        {
            strip.Hide();
            return;
        }

        strip.Texture = _atlasesByLevel[level - 1];
        strip.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        strip.StretchMode = TextureRect.StretchModeEnum.Scale;
        ApplyLayoutRightAnchoredBelowBanner(strip, banner, level);
        strip.Show();
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
