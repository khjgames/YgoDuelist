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
/// Renders YGO level stars, attribute icon, and race icon on monster cards (under title banner, right-aligned).
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoMonsterLevelStripPatch
{
    private const string StarsStripPath = "YgoDuelist/images/card_frames/12_stars.png";
    private const string AttributeIconFolder = "YgoDuelist/images/card_frames/Attribute";
    private const string RaceIconFolder = "YgoDuelist/images/card_frames/Race";
    private const string StripNodeName = "YgoLevelStarsStrip";
    private const string AttributeNodeName = "YgoAttributeIcon";
    private const string RaceNodeName = "YgoRaceIcon";

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

    /// <summary>Gap between the bottom of the level strip and the top of the attribute/race icon row.</summary>
    private const float AttributeGapBelowLevelStripPx = 2f;

    /// <summary>Attribute and race icon row height (width follows texture aspect).</summary>
    private const float IconRowHeightPx = 24f;

    /// <summary>Extra vertical offset after gap below level strip (negative = further up).</summary>
    private const float IconRowExtraVerticalNudgePx = 0f;

    /// <summary>Race icon: horizontal nudge from card/banner right (positive = move left), same sense as <see cref="StripHorizontalNudgePx"/>.</summary>
    private const float RaceHorizontalNudgePx = 48f;

    /// <summary>Attribute sits to the left of the race; its right edge is this many px left of the race’s right edge.</summary>
    private const float AttributeRightEdgeLeftOfRaceRightPx = 30f;

    private static Texture2D? _stripTexture;
    private static AtlasTexture[]? _atlasesByLevel;
    private static readonly Dictionary<DuelMonsterAttribute, Texture2D?> _attributeTextures = new();
    private static readonly Dictionary<DuelMonsterRace, Texture2D?> _raceTextures = new();

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        Control body = __instance.Body;
        var strip = body.GetNodeOrNull<TextureRect>(StripNodeName);
        var attributeIcon = body.GetNodeOrNull<TextureRect>(AttributeNodeName);
        var raceIcon = body.GetNodeOrNull<TextureRect>(RaceNodeName);

        CardModel? model = __instance.Model;
        if (model == null || model.Rarity == CardRarity.Ancient || model is not AbstractMonsterCard monster)
        {
            strip?.Hide();
            attributeIcon?.Hide();
            raceIcon?.Hide();
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
            raceIcon?.Hide();
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
        TextureRect raceNode = EnsureRaceIconNode(body, attrNode);
        UpdateAttributeIcon(attrNode, monster, banner);
        UpdateRaceIcon(raceNode, monster, banner);
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

    private static TextureRect EnsureRaceIconNode(Control body, TextureRect attributeNode)
    {
        var race = body.GetNodeOrNull<TextureRect>(RaceNodeName);
        if (race != null)
            return race;

        race = new TextureRect
        {
            Name = RaceNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
        body.AddChild(race);
        int insertAt = attributeNode.GetIndex() + 1;
        body.MoveChild(race, insertAt);
        return race;
    }

    private static float GetIconRowTop(TextureRect banner)
    {
        float levelStripTop = banner.OffsetBottom + StripVerticalNudgePx;
        return levelStripTop + StripHeightPx + AttributeGapBelowLevelStripPx + IconRowExtraVerticalNudgePx;
    }

    private static void ApplyIconRowAnchors(TextureRect rect, TextureRect banner)
    {
        rect.AnchorLeft = 0.5f;
        rect.AnchorRight = 0.5f;
        rect.AnchorTop = banner.AnchorTop;
        rect.AnchorBottom = banner.AnchorBottom;
        float top = GetIconRowTop(banner);
        rect.OffsetTop = top;
        rect.OffsetBottom = top + IconRowHeightPx;
    }

    private static void LayoutIconInRow(TextureRect rect, TextureRect banner, Texture2D tex, float rightEdgeOffsetFromBannerRight)
    {
        ApplyIconRowAnchors(rect, banner);

        Vector2 szf = tex.GetSize();
        int tw = (int)szf.X;
        int th = (int)szf.Y;
        if (tw <= 0 || th <= 0)
            return;

        float displayW = IconRowHeightPx * (tw / (float)th);
        float bannerW = banner.OffsetRight - banner.OffsetLeft;
        if (displayW > bannerW)
            displayW = bannerW;

        float right = banner.OffsetRight - rightEdgeOffsetFromBannerRight;
        rect.OffsetRight = right;
        rect.OffsetLeft = right - displayW;
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

        LayoutIconInRow(attr, banner, tex, RaceHorizontalNudgePx + AttributeRightEdgeLeftOfRaceRightPx);
        attr.Show();
    }

    private static void UpdateRaceIcon(TextureRect race, AbstractMonsterCard monster, TextureRect banner)
    {
        Texture2D? tex = GetRaceTexture(monster.DuelMonsterRace);
        if (tex == null)
        {
            race.Hide();
            return;
        }

        race.Texture = tex;
        race.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        race.StretchMode = TextureRect.StretchModeEnum.Scale;

        LayoutIconInRow(race, banner, tex, RaceHorizontalNudgePx);
        race.Show();
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

    private static string GetRaceIconFileName(DuelMonsterRace race) =>
        race switch
        {
            DuelMonsterRace.BeastWarrior => "Beast-Warrior.png",
            DuelMonsterRace.DivineBeast => "Divine-Beast.png",
            DuelMonsterRace.SeaSerpent => "Sea Serpent.png",
            DuelMonsterRace.WingedBeast => "Winged Beast.png",
            DuelMonsterRace.SpellNormal => "Spellcaster.png",
            DuelMonsterRace.SpellContinuous => "Continuous.png",
            DuelMonsterRace.SpellQuickPlay => "Quick-Play.png",
            DuelMonsterRace.SpellEquip => "Equip.png",
            DuelMonsterRace.SpellField => "Field.png",
            DuelMonsterRace.SpellRitual => "Ritual.png",
            DuelMonsterRace.TrapNormal => "Spellcaster.png",
            DuelMonsterRace.TrapContinuous => "Continuous.png",
            DuelMonsterRace.TrapCounter => "Counter.png",
            _ => $"{race}.png",
        };

    private static Texture2D? GetRaceTexture(DuelMonsterRace race)
    {
        if (_raceTextures.TryGetValue(race, out Texture2D? cached))
            return cached;

        string path = $"{RaceIconFolder}/{GetRaceIconFileName(race)}";
        Texture2D? loaded = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        _raceTextures[race] = loaded;
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
