using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Renders YGO race icon on spell/trap cards (single icon under the title banner).
/// Monsters use <see cref="YgoMonsterLevelStripPatch"/> instead.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoSpellTrapRaceIconPatch
{
    private const string RaceIconFolder = "YgoDuelist/images/card_frames/Race";
    private const string RaceNodeName = "YgoRaceIcon";

    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    private const float IconRowHeightPx = 30f;
    private const float RaceHorizontalNudgePx = 48f;

    private const float RaceTopGapBelowBannerPx = -26f;
    private const float RaceSetAlpha = 0.9f;

    private static readonly Dictionary<DuelMonsterRace, Texture2D?> _raceTextures = new();
    private static bool _hoverTipLogOnce;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        Control body = __instance.Body;
        if (body == null)
            return;

        CardModel? model = __instance.Model;
        if (model == null)
            return;
        bool useSetTransparency = YgoSetCardVisualHelper.ShouldUseSetFrame(model);

        // Monsters are handled by YgoMonsterLevelStripPatch.
        if (model is AbstractMonsterCard)
            return;

        if (model is not IYgoCard ygo)
            return;

        // Default/no-icon races for which we intentionally do not render an icon.
        // (Normal spells/traps have their own styling already and would look redundant.)
        if (ygo.DuelMonsterRace == DuelMonsterRace.Warrior ||
            ygo.DuelMonsterRace == DuelMonsterRace.SpellNormal ||
            ygo.DuelMonsterRace == DuelMonsterRace.TrapNormal)
        {
            TextureRect? existingRaceIcon = body.GetNodeOrNull<TextureRect>(RaceNodeName);
            existingRaceIcon?.Hide();
            return;
        }

        var banner = body.GetNodeOrNull<TextureRect>("%TitleBanner");
        if (banner == null)
            return;

        TextureRect? raceIcon = body.GetNodeOrNull<TextureRect>(RaceNodeName);
        if (raceIcon == null)
        {
            raceIcon = new TextureRect
            {
                Name = RaceNodeName,
                // Must be Stop to receive hover events.
                MouseFilter = Control.MouseFilterEnum.Stop,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both,
            };
            body.AddChild(raceIcon);
            body.MoveChild(raceIcon, banner.GetIndex() + 1);
        }

        Texture2D? tex = GetRaceTexture(ygo.DuelMonsterRace);
        if (tex == null)
        {
            raceIcon.Hide();
            return;
        }

        raceIcon.Texture = tex;
        raceIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        raceIcon.StretchMode = TextureRect.StretchModeEnum.Scale;
        raceIcon.Modulate = new Color(1f, 1f, 1f, useSetTransparency ? RaceSetAlpha : 1f);

        // Anchor + offsets in card-local space, similar to YgoMonsterLevelStripPatch.
        ApplyIconRowAnchors(raceIcon, banner);
        LayoutIconInRow(raceIcon, banner, tex, RaceHorizontalNudgePx);
        raceIcon.Show();
        TrySetHoverTip(raceIcon, GetRaceHoverTipKey(ygo.DuelMonsterRace));
    }

    private static void ApplyIconRowAnchors(TextureRect rect, TextureRect banner)
    {
        rect.AnchorLeft = 0.5f;
        rect.AnchorRight = 0.5f;
        rect.AnchorTop = banner.AnchorTop;
        rect.AnchorBottom = banner.AnchorBottom;

        float top = banner.OffsetBottom + RaceTopGapBelowBannerPx;
        rect.OffsetTop = top;
        rect.OffsetBottom = top + IconRowHeightPx;
    }

    private static void LayoutIconInRow(
        TextureRect rect,
        TextureRect banner,
        Texture2D tex,
        float rightEdgeOffsetFromBannerRight)
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

    private static Texture2D? GetRaceTexture(DuelMonsterRace race)
    {
        if (_raceTextures.TryGetValue(race, out Texture2D? cached))
            return cached;

        string path = $"{RaceIconFolder}/{GetRaceIconFileName(race)}";
        Texture2D? loaded = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        _raceTextures[race] = loaded;
        return loaded;
    }

    private static string GetRaceIconFileName(DuelMonsterRace race) =>
        race switch
        {
            DuelMonsterRace.SpellContinuous => "Continuous.png",
            DuelMonsterRace.SpellQuickPlay => "Quick-Play.png",
            DuelMonsterRace.SpellEquip => "Equip.png",
            DuelMonsterRace.SpellField => "Field.png",
            DuelMonsterRace.SpellRitual => "Ritual.png",
            DuelMonsterRace.TrapContinuous => "Continuous.png",
            DuelMonsterRace.TrapCounter => "Counter.png",
            _ => $"{race}.png",
        };

    private static void TrySetHoverTip(TextureRect icon, string hoverTipKey)
    {
        try
        {
            icon.MouseFilter = Control.MouseFilterEnum.Stop;

            var title = new LocString("static_hover_tips", hoverTipKey + ".title");
            var description = new LocString("static_hover_tips", hoverTipKey + ".description");
            description.Add("conduitIcon", ConduitImgBbcode);
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
}

