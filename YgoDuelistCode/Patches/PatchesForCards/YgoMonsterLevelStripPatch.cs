using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Renders YGO level stars, attribute icon, and race icon on monster cards (under title banner, right-aligned).
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoMonsterLevelStripPatch
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    private const string StarsStripPath = "YgoDuelist/images/card_frames/12_stars.png";
    private const string StarsStripFaceDownPath = "YgoDuelist/images/card_frames/12_stars_facedown.png";
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
    private const float IconRowHeightPx = 26f;

    /// <summary>Extra vertical offset after gap below level strip (negative = further up).</summary>
    private const float IconRowExtraVerticalNudgePx = 2f;

    /// <summary>Race icon: horizontal nudge from card/banner right (positive = move left), same sense as <see cref="StripHorizontalNudgePx"/>.</summary>
    private const float RaceHorizontalNudgePx = 48f;
    /// <summary>Command-card portraits need race icon slightly lower than normal cards.</summary>
    private const float CommandCardRaceVerticalNudgePx = 48f;

    /// <summary>Attribute sits to the left of the race; its right edge is this many px left of the race’s right edge.</summary>
    private const float AttributeRightEdgeLeftOfRaceRightPx = 31f;
    private const float AttributeSetAlpha = 0.75f;
    private const float RaceSetAlpha = 0.9f;

    private static Texture2D? _stripTextureFaceUp;
    private static Texture2D? _stripTextureFaceDown;
    private static AtlasTexture[]? _atlasesByLevelFaceUp;
    private static AtlasTexture[]? _atlasesByLevelFaceDown;
    private static readonly Dictionary<DuelMonsterAttribute, Texture2D?> _attributeTextures = new();
    private static readonly Dictionary<DuelMonsterRace, Texture2D?> _raceTextures = new();
    private static readonly HashSet<int> _loggedStripDebugIds = new();
    private static bool _hoverTipLogOnce;

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
        if (model == null || model.Rarity == CardRarity.Ancient)
        {
            strip?.Hide();
            attributeIcon?.Hide();
            raceIcon?.Hide();
            return;
        }

        if (!TryGetMonsterVisualData(model, out var attribute, out var race, out int level, out bool useFaceDownStrip))
        {
            strip?.Hide();
            attributeIcon?.Hide();
            raceIcon?.Hide();
            return;
        }

        LogStripVisualStateOnce(model, useFaceDownStrip);
        Texture2D? stripTexture = GetStripTexture(useFaceDownStrip);
        AtlasTexture[]? atlasesByLevel = EnsureAtlases(stripTexture, useFaceDownStrip);

        if (stripTexture == null || atlasesByLevel == null)
        {
            strip?.Hide();
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

        level = Mathf.Clamp(level, 1, 12);
        if (level < 1 || level > 12 || atlasesByLevel == null || stripTexture == null)
        {
            strip?.Hide();
        }
        else
        {
            strip!.Texture = atlasesByLevel[level - 1];
            strip.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            strip.StretchMode = TextureRect.StretchModeEnum.Scale;
            ApplyLayoutRightAnchoredBelowBanner(strip, banner, stripTexture, level);
            strip.Show();
        }

        TextureRect attrNode = EnsureAttributeIconNode(body, banner, strip);
        TextureRect raceNode = EnsureRaceIconNode(body, attrNode);
        UpdateAttributeIcon(attrNode, attribute, model, banner, useFaceDownStrip);
        UpdateRaceIcon(raceNode, race, model, banner, useFaceDownStrip);
    }

    private static TextureRect EnsureAttributeIconNode(Control body, TextureRect banner, TextureRect? strip)
    {
        var attr = body.GetNodeOrNull<TextureRect>(AttributeNodeName);
        if (attr != null)
            return attr;

        attr = new TextureRect
        {
            Name = AttributeNodeName,
            // Must be Stop to receive hover events.
            MouseFilter = Control.MouseFilterEnum.Stop,
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
            // Must be Stop to receive hover events.
            MouseFilter = Control.MouseFilterEnum.Stop,
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

    private static void UpdateAttributeIcon(TextureRect attr, DuelMonsterAttribute attribute, CardModel model, TextureRect banner, bool useSetTransparency)
    {
        Texture2D? tex = GetAttributeTexture(attribute);
        if (tex == null)
        {
            attr.Hide();
            return;
        }

        attr.Texture = tex;
        attr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        attr.StretchMode = TextureRect.StretchModeEnum.Scale;
        attr.Modulate = new Color(1f, 1f, 1f, useSetTransparency ? AttributeSetAlpha : 1f);

        LayoutIconInRow(attr, banner, tex, RaceHorizontalNudgePx + AttributeRightEdgeLeftOfRaceRightPx);
        attr.Show();
        TrySetHoverTip(attr, GetAttributeHoverTipKey(attribute), model);
    }

    private static void UpdateRaceIcon(TextureRect race, DuelMonsterRace raceType, CardModel model, TextureRect banner, bool useSetTransparency)
    {
        Texture2D? tex = GetRaceTexture(raceType);
        if (tex == null)
        {
            race.Hide();
            return;
        }

        race.Texture = tex;
        race.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        race.StretchMode = TextureRect.StretchModeEnum.Scale;
        race.Modulate = new Color(1f, 1f, 1f, useSetTransparency ? RaceSetAlpha : 1f);

        LayoutIconInRow(race, banner, tex, RaceHorizontalNudgePx);
        if (model is MonsterCommandCard)
        {
            race.OffsetTop += CommandCardRaceVerticalNudgePx;
            race.OffsetBottom += CommandCardRaceVerticalNudgePx;
        }
        race.Show();
        TrySetHoverTip(race, GetRaceHoverTipKey(raceType), model);
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

    private static void TrySetHoverTip(TextureRect icon, string hoverTipKey, CardModel model)
    {
        try
        {
            // Allow hover detection on the icon node itself.
            icon.MouseFilter = Control.MouseFilterEnum.Stop;

            var title = new LocString("static_hover_tips", hoverTipKey + ".title");
            var description = new LocString("static_hover_tips", hoverTipKey + ".description");
            bool useConduitIcon = model is IYgoCard ygo
                                  && ygo.YgoCardType != YgoCardType.FusionMonster
                                  && ygo.YgoCardType != YgoCardType.RitualMonster;
            description.Add("conduitIcon", useConduitIcon ? ConduitImgBbcode : string.Empty);
            var tip = new HoverTip(title, description);
            Traverse.Create(icon).Field("_hoverTip").SetValue(tip);
        }
        catch (System.Exception e)
        {
            if (!_hoverTipLogOnce)
            {
                _hoverTipLogOnce = true;
                GD.Print($"[YgoDuelist] Failed to set hover tip for '{hoverTipKey}': {e.Message}");
            }
        }
    }

    private static string GetAttributeHoverTipKey(DuelMonsterAttribute attribute) =>
        $"YGO_ATTRIBUTE_{attribute.ToString().ToUpperInvariant()}";

    private static string GetRaceHoverTipKey(DuelMonsterRace race) =>
        $"YGO_RACE_{ToScreamingSnake(race.ToString())}";

    private static string ToScreamingSnake(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var chars = value.ToCharArray();
        var outChars = new List<char>(chars.Length * 2);

        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (char.IsUpper(c))
            {
                bool prevIsLowerOrDigit = i > 0 && (char.IsLower(chars[i - 1]) || char.IsDigit(chars[i - 1]));
                if (prevIsLowerOrDigit)
                    outChars.Add('_');

                outChars.Add(char.ToUpperInvariant(c));
            }
            else
            {
                outChars.Add(char.ToUpperInvariant(c));
            }
        }

        return new string(outChars.ToArray());
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

    private static Texture2D? GetStripTexture(bool useFaceDownStrip)
    {
        if (useFaceDownStrip)
        {
            _stripTextureFaceDown ??= ResourceLoader.Load<Texture2D>(StarsStripFaceDownPath, null, ResourceLoader.CacheMode.Reuse);
            return _stripTextureFaceDown;
        }

        _stripTextureFaceUp ??= ResourceLoader.Load<Texture2D>(StarsStripPath, null, ResourceLoader.CacheMode.Reuse);
        return _stripTextureFaceUp;
    }

    private static void LogStripVisualStateOnce(CardModel model, bool useFaceDownStrip)
    {
        if (model.Pile?.Type != PileType.Hand)
            return;

        int key = model.GetHashCode();
        if (!_loggedStripDebugIds.Add(key))
            return;

        if (!TryGetMonsterVisualData(model, out var attribute, out var race, out _, out _))
            return;

        bool isHandEffect = model is AbstractMonsterCard m && m.IsHandEffectFormActive;
        bool willSet = model is AbstractMonsterCard m2 && m2.WillSet;
        bool faceDown = model is AbstractMonsterCard m3 && m3.FaceDown;
        YgoCardType type = model is IYgoCard ygo ? ygo.YgoCardType : YgoCardType.Monster;
        GD.Print(
            $"[YgoStripVisualDebug] Id={model.Id.Entry}, YgoCardType={type}, Attr={attribute}, Race={race}, IsHandEffectFormActive={isHandEffect}, WillSet={willSet}, FaceDown={faceDown}, UseFaceDownStrip={useFaceDownStrip}");
    }

    private static bool TryGetMonsterVisualData(
        CardModel model,
        out DuelMonsterAttribute attribute,
        out DuelMonsterRace race,
        out int level,
        out bool useFaceDownStrip)
    {
        if (model is AbstractMonsterCard monster)
        {
            attribute = monster.DuelMonsterAttribute;
            race = monster.DuelMonsterRace;
            level = monster is BaseMonsterCard bm ? bm.GetEffectiveDuelMonsterLevel() : monster.DuelMonsterLevel;
            useFaceDownStrip = monster.FaceDown
                               || (monster.Pile?.Type == PileType.Hand
                                   && monster.WillSet
                                   && !monster.IsAttackBattlePosition
                                   && !monster.IsHandEffectFormActive
                                   && monster.YgoCardType != YgoCardType.FusionMonster);
            return true;
        }

        if (model is MonsterCommandCard cmd && cmd.SourceMonster != null)
        {
            var src = cmd.SourceMonster;
            attribute = src.DuelMonsterAttribute;
            race = src.DuelMonsterRace;
            level = src.GetEffectiveDuelMonsterLevel();
            useFaceDownStrip = false;
            return true;
        }

        attribute = DuelMonsterAttribute.Earth;
        race = DuelMonsterRace.Warrior;
        level = 1;
        useFaceDownStrip = false;
        return false;
    }

    private static AtlasTexture[]? EnsureAtlases(Texture2D? strip, bool useFaceDownStrip)
    {
        if (strip == null)
            return null;

        AtlasTexture[]? current = useFaceDownStrip ? _atlasesByLevelFaceDown : _atlasesByLevelFaceUp;
        if (current != null)
            return current;

        Vector2 szf = strip.GetSize();
        int tw = (int)szf.X;
        int th = (int)szf.Y;
        if (tw <= 0 || th <= 0)
            return null;

        int cellW = tw / 12;
        if (cellW <= 0)
            return null;

        var atlases = new AtlasTexture[12];
        for (int i = 0; i < 12; i++)
        {
            int regionW = cellW * (i + 1);
            var atlas = new AtlasTexture
            {
                Atlas = strip,
                Region = new Rect2I(0, 0, regionW, th),
            };
            atlases[i] = atlas;
        }

        if (useFaceDownStrip)
            _atlasesByLevelFaceDown = atlases;
        else
            _atlasesByLevelFaceUp = atlases;

        return atlases;
    }

    /// <summary>
    /// Same vertical band as the title banner; horizontal strip is width = aspect-fit for current level,
    /// right edge aligned with the banner’s right edge (YGO-style).
    /// </summary>
    private static void ApplyLayoutRightAnchoredBelowBanner(TextureRect strip, TextureRect banner, Texture2D stripTexture, int level)
    {
        strip.AnchorLeft = 0.5f;
        strip.AnchorRight = 0.5f;
        strip.AnchorTop = banner.AnchorTop;
        strip.AnchorBottom = banner.AnchorBottom;

        float top = banner.OffsetBottom + StripVerticalNudgePx;
        strip.OffsetTop = top;
        strip.OffsetBottom = top + StripHeightPx;

        Vector2 szf = stripTexture.GetSize();
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
